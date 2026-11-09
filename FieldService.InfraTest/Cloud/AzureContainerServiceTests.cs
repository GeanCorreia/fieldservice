using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers.Models;
using FieldService.Cloud.Entities;
using FieldService.Cloud.Services;
using FieldService.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace FieldService.InfraTest.Cloud;

public class AzureContainerServiceTests
{
    private const string Subscription = "00000000-0000-0000-0000-000000000001";
    private const string ResourceId = $"/subscriptions/{Subscription}/resourceGroups/test/providers/Microsoft.App/containerApps/test";

    [Theory]
    [InlineData(ExecutionType.OnDemand, 0, 1, 1)]
    [InlineData(ExecutionType.AlwaysOn, 1, 4, 0)]
    public void ScaleConfigurationPreservesExecutionPolicy(ExecutionType type, int min, int max, int rules)
    {
        using var fixture = new Fixture();
        var service = new AzureContainerConfigurationService(fixture.Service);

        var scale = service.CreateScaleConfiguration(new ContainerConfiguration(type, 4, 0), "UTC");

        Assert.Equal(min, scale.MinReplicas);
        Assert.Equal(max, scale.MaxReplicas);
        Assert.Equal(rules, scale.Rules.Count);
        if (type == ExecutionType.OnDemand)
            Assert.Equal("10", Assert.Single(scale.Rules).Http.Metadata["concurrentRequests"]);
    }

    [Fact]
    public void ScheduledScaleUsesProvidedTimezoneAndOnlyConfiguredDays()
    {
        using var fixture = new Fixture();
        var service = new AzureContainerConfigurationService(fixture.Service);
        var window = new ExecutionWindow(
            new DailyExecutionWindow(new TimeOnly(8, 15), new TimeOnly(18, 30)),
            null, null, null, null, null,
            new DailyExecutionWindow(new TimeOnly(9, 0), new TimeOnly(12, 0)));

        var scale = service.CreateScaleConfiguration(
            new ContainerConfiguration(ExecutionType.Scheduled, 4, 0, window), "Europe/Lisbon");

        Assert.Equal(0, scale.MinReplicas);
        Assert.Equal(4, scale.MaxReplicas);
        Assert.Equal(3, scale.Rules.Count);
        var monday = Assert.Single(scale.Rules, rule => rule.Name == "schedule-monday");
        Assert.Equal("cron", monday.Custom.CustomScaleRuleType);
        Assert.Equal("Europe/Lisbon", monday.Custom.Metadata["timezone"]);
        Assert.Equal("15 8 * * 1", monday.Custom.Metadata["start"]);
        Assert.Equal("30 18 * * 1", monday.Custom.Metadata["end"]);
        Assert.Equal("1", monday.Custom.Metadata["desiredReplicas"]);
        var sunday = Assert.Single(scale.Rules, rule => rule.Name == "schedule-sunday");
        Assert.Equal("0 9 * * 0", sunday.Custom.Metadata["start"]);
    }

    [Theory]
    [InlineData("Succeeded", 1, ContainerInstanceStatus.Running)]
    [InlineData("Succeeded", 0, ContainerInstanceStatus.Stopped)]
    [InlineData("Failed", 1, ContainerInstanceStatus.Failed)]
    [InlineData("In-Progress", 0, ContainerInstanceStatus.Provisioning)]
    [InlineData("Deleting", 0, ContainerInstanceStatus.Deleting)]
    [InlineData("Canceled", 0, ContainerInstanceStatus.Canceled)]
    [InlineData("", 0, ContainerInstanceStatus.Unknown)]
    [InlineData("", 1, ContainerInstanceStatus.Provisioning)]
    public async Task StatusIsMappedIndependentlyOfContainerApplication(
        string state, int minReplicas, ContainerInstanceStatus expected)
    {
        using var fixture = new Fixture(state, minReplicas);

        Assert.Equal(expected, await fixture.Service.GetStatusAsync(ResourceId));
        Assert.Equal(ResourceId, fixture.Service.ResourceId("test"));
    }

    [Theory]
    [InlineData(ExecutionType.OnDemand, true, 0)]
    [InlineData(ExecutionType.OnDemand, false, 0)]
    [InlineData(ExecutionType.Scheduled, true, 0)]
    [InlineData(ExecutionType.Scheduled, false, 0)]
    [InlineData(ExecutionType.AlwaysOn, true, 1)]
    [InlineData(ExecutionType.AlwaysOn, false, 1)]
    public void LifecycleScaleTargetsArePreserved(ExecutionType type, bool scaleUp, int min)
    {
        Assert.Equal((min, 1), AzureContainerService.ResolveScaleTarget(type, scaleUp));
    }

    [Fact]
    public async Task CreateUsesSharedConnectionOptionsAndCallerWorkload()
    {
        using var fixture = new Fixture();
        var resource = await fixture.Service.CreateAsync(
            "test",
            new ContainerAppContainer
            {
                Name = "test",
                Image = "example/app:1",
                Command = { "/bin/sh", "-c", "start-app" },
                Env = { new ContainerAppEnvironmentVariable { Name = "APP_SETTING", Value = "value" } }
            },
            new ContainerAppScale { MinReplicas = 1, MaxReplicas = 2 },
            8088);

        Assert.Equal(ResourceId, resource.Id.ToString());
        var request = Assert.Single(fixture.Handler.Requests, request => request.Method == HttpMethod.Put);
        using var document = JsonDocument.Parse(request.Body!);
        var properties = document.RootElement.GetProperty("properties");
        Assert.Equal("eastus", document.RootElement.GetProperty("location").GetString());
        Assert.Equal($"/subscriptions/{Subscription}/resourceGroups/test/providers/Microsoft.App/managedEnvironments/environment",
            properties.GetProperty("environmentId").GetString());
        Assert.Equal(8088, properties.GetProperty("configuration").GetProperty("ingress").GetProperty("targetPort").GetInt32());
        var template = properties.GetProperty("template");
        Assert.Equal(2, template.GetProperty("scale").GetProperty("maxReplicas").GetInt32());
        var container = template.GetProperty("containers")[0];
        Assert.Equal("example/app:1", container.GetProperty("image").GetString());
        Assert.Equal("start-app", container.GetProperty("command")[2].GetString());
        Assert.Equal("value", container.GetProperty("env")[0].GetProperty("value").GetString());
    }

    [Fact]
    public async Task ApplyingUnchangedScaleDoesNotSendUpdate()
    {
        using var fixture = new Fixture();
        var configuration = new AzureContainerConfigurationService(fixture.Service);

        await configuration.ApplyAsync(ResourceId, new ContainerConfiguration(ExecutionType.AlwaysOn, 1, 1), "UTC");

        Assert.DoesNotContain(fixture.Handler.Requests, request => request.Method != HttpMethod.Get);
    }

    [Fact]
    public async Task ApplyingChangedScaleUpdatesTheResource()
    {
        using var fixture = new Fixture();
        var configuration = new AzureContainerConfigurationService(fixture.Service);

        await configuration.ApplyAsync(ResourceId, new ContainerConfiguration(ExecutionType.AlwaysOn, 3, 1), "UTC");

        var request = Assert.Single(fixture.Handler.Requests, request => request.Method != HttpMethod.Get);
        using var document = JsonDocument.Parse(request.Body!);
        Assert.Equal(3, document.RootElement.GetProperty("properties")
            .GetProperty("template").GetProperty("scale").GetProperty("maxReplicas").GetInt32());
    }

    [Fact]
    public async Task FqdnResolutionNormalizesTheUrl()
    {
        using var fixture = new Fixture();
        var resource = await fixture.Service.GetAsync(ResourceId);

        Assert.Equal("https://test.example.com",
            await fixture.Service.ResolveFqdnUrlAsync(resource, 2, TimeSpan.Zero));
    }

    [Fact]
    public async Task FqdnResolutionStopsAtTheConfiguredAttemptLimit()
    {
        using var fixture = new Fixture();
        var resource = await fixture.Service.GetAsync(ResourceId);
        fixture.Handler.Fqdn = null;
        fixture.Handler.Requests.Clear();

        await Assert.ThrowsAsync<TimeoutException>(
            () => fixture.Service.ResolveFqdnUrlAsync(resource, 3, TimeSpan.Zero));
        Assert.Equal(3, fixture.Handler.Requests.Count);
    }

    [Fact]
    public async Task LifecycleScaleUpdatesReplicaBoundsThroughCloud()
    {
        using var fixture = new Fixture();

        await fixture.Service.ApplyExecutionScaleAsync(ResourceId, ExecutionType.Scheduled, isScaleUp: false);

        var request = Assert.Single(fixture.Handler.Requests, request => request.Method != HttpMethod.Get);
        using var document = JsonDocument.Parse(request.Body!);
        var scale = document.RootElement.GetProperty("properties").GetProperty("template").GetProperty("scale");
        Assert.Equal(0, scale.GetProperty("minReplicas").GetInt32());
        Assert.Equal(1, scale.GetProperty("maxReplicas").GetInt32());
    }

    [Fact]
    public async Task MissingResourceReturnsNullButPermissionErrorsPropagate()
    {
        using var fixture = new Fixture();
        fixture.Handler.StatusCode = HttpStatusCode.NotFound;
        Assert.Null(await fixture.Service.FindByNameAsync("test"));

        fixture.Handler.StatusCode = HttpStatusCode.Forbidden;
        var exception = await Assert.ThrowsAsync<Azure.RequestFailedException>(
            () => fixture.Service.FindByNameAsync("test"));
        Assert.Equal(403, exception.Status);
    }

    private sealed class Fixture : IDisposable
    {
        public TestHandler Handler { get; }
        public AzureContainerService Service { get; }
        private readonly HttpClient _client;

        public Fixture(string state = "Succeeded", int minReplicas = 1)
        {
            Handler = new TestHandler(state, minReplicas);
            _client = new HttpClient(Handler);
            var options = new ArmClientOptions { Transport = new HttpClientTransport(_client) };
            options.Retry.MaxRetries = 0;
            options.SetApiVersion("Microsoft.App/containerApps", "2025-01-01");
            Service = new AzureContainerService(
                new ArmClient(new TestCredential(), Subscription, options),
                Options.Create(new AzureIdentityOptions
                {
                    AzureSubscriptionId = Subscription,
                    AzureResourceGroupName = "test",
                    AzureLocation = "eastus",
                    AzureContainerAppEnvironmentId =
                        $"/subscriptions/{Subscription}/resourceGroups/test/providers/Microsoft.App/managedEnvironments/environment"
                }));
        }

        public void Dispose() => _client.Dispose();
    }

    private sealed class TestCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new("test-token", DateTimeOffset.UtcNow.AddHours(1));
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }

    private sealed class TestHandler(string state, int minReplicas) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string? Body)> Requests { get; } = [];
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string? Fqdn { get; set; } = "test.example.com";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            var json = JsonSerializer.Serialize(new
            {
                id = ResourceId,
                name = "test",
                type = "Microsoft.App/containerApps",
                location = "eastus",
                properties = new
                {
                    provisioningState = state,
                    configuration = new { ingress = new { fqdn = Fqdn, external = true, targetPort = 8088 } },
                    template = new { scale = new { minReplicas, maxReplicas = 1 } }
                }
            });
            return new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
