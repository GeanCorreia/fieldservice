using FieldService.Broker;
using FieldService.Broker.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.InfraTest.Broker;

public sealed class BrokerConnectionTests
{
    [Fact]
    public void Should_connect_to_rabbitmq_and_resolve_public_services()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:RabbitMQ"] = "amqp://guest:guest@localhost:5672/"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IMessageProcessingLock, NoopMessageProcessingLock>();
        services.AddBrokerModule(configuration);

        using var provider = services.BuildServiceProvider();

        var persistentConnection = provider.GetRequiredService<IRabbitMqPersistentConnection>();
        var producer = provider.GetRequiredService<IMessageProducer>();
        var consumer = provider.GetRequiredService<IMessageConsumer>();

        Assert.NotNull(producer);
        Assert.NotNull(consumer);
        Assert.True(persistentConnection.GetConnection().IsOpen);
    }

    private sealed class NoopMessageProcessingLock : IMessageProcessingLock
    {
        public Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default) => Task.FromResult(true);
        public Task ReleaseAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }
}
