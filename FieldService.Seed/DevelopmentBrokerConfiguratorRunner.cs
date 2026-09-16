using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FieldService.Broker.Configuration;
using FieldService.Broker.Extensions;
using FieldService.Broker.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FieldService.ConsoleTests;

public static class DevelopmentBrokerConfiguratorRunner
{
    private const string DefaultLocalConfigDirectory = "docker/ServiceBusEmulator";
    private const string DefaultLocalConfigFileName = "Config.json";

    public static async Task Run(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddSimpleConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var repoRoot = FindRepositoryRoot();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(repoRoot)
            .AddJsonFile(Path.Combine("FieldService.Api", "appsettings.Development.json"), optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetSection(AzureServiceBusOptions.SectionName)
            .Get<AzureServiceBusOptions>()?.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("AzureServiceBus:ConnectionString não configurada.");

        services.AddSingleton(new ServiceBusClient(connectionString));

        await using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DevelopmentBrokerConfiguratorRunner));

        // 1. Descobre a topologia real da solução
        var topologies = BrokerTopologyDiscoverer.DiscoverBrokerTopologies().ToList();

        // 2. Monta o JSON já sanitizado com os limites do emulador local
        await GenerateSanitizedEmulatorConfigAsync(topologies, repoRoot, logger);

        logger.LogInformation("Service Bus emulator config updated.");

        // 3. Valida a conexão AMQP (Porta 5672)
        await ValidateEmulatorConnectionAsync(serviceProvider, logger, cancellationToken);

        Console.WriteLine("Pressione ENTER para finalizar...");
        Console.ReadLine();
    }

    private static async Task GenerateSanitizedEmulatorConfigAsync(
        IEnumerable<BrokerTopology> topologies, 
        string repoRoot, 
        ILogger logger, 
        string namespaceName = "sbemulatorns")
    {
        var topics = topologies.Select(top =>
        {
            
            var subsList = top.Subscriptions.Any() 
                ? top.Subscriptions.ToList() 
                : new List<string> { "dummy-dev.sub" };

            return new
            {
                Name = top.EntityName,
                Properties = new Dictionary<string, object>
                {
                    // Limite máximo aceito pelo emulador: PT1H
                    ["DefaultMessageTimeToLive"] = "PT1H" 
                },
                Subscriptions = subsList.Select(subName => new
                {
                    Name = subName,
                    Properties = new Dictionary<string, object>
                    {
                        ["DeadLetteringOnMessageExpiration"] = true,
                        // Limite máximo aceito pelo emulador: PT5M (usando PT1M como segurança)
                        ["LockDuration"] = "PT1M",
                        ["MaxDeliveryCount"] = 10,
                        ["RequiresSession"] = false
                    }
                }).ToList()
            };
        }).ToList();

        var config = new
        {
            UserConfig = new
            {
                Namespaces = new[]
                {
                    new
                    {
                        Name = namespaceName,
                        Topics = topics,
                        Queues = Array.Empty<object>()
                    }
                },
                Logging = new { Type = "File" }
            }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        var targetPath = Path.Combine(repoRoot, DefaultLocalConfigDirectory, DefaultLocalConfigFileName);

        var directoryPath = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(targetPath, $"{json}{Environment.NewLine}");
        logger.LogInformation("Arquivo de configuração do Service Bus Emulator gerado em: {TargetPath}", targetPath);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "FieldService.Api", "appsettings.Development.json");
            if (File.Exists(candidate))
                return current.FullName;

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static async Task ValidateEmulatorConnectionAsync(IServiceProvider serviceProvider, ILogger logger, CancellationToken ct)
    {
        var client = serviceProvider.GetRequiredService<ServiceBusClient>();
        var brokerTopologies = BrokerTopologyDiscoverer.DiscoverBrokerTopologies().ToList();

        logger.LogInformation("Aguardando porta AMQP do emulador (5672)...");
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        foreach (var topology in brokerTopologies)
        {
            await RetryPolicyAsync(async () =>
            {
                await using var sender = client.CreateSender(topology.EntityName);
            }, maxRetries: 5, delay: TimeSpan.FromSeconds(2));
        }

        logger.LogInformation("Conexão e topologia validadas via AMQP.");
    }

    private static async Task RetryPolicyAsync(Func<Task> action, int maxRetries, TimeSpan delay)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                await action();
                return;
            }
            catch when (i < maxRetries - 1)
            {
                await Task.Delay(delay);
            }
        }

        await action();
    }
}