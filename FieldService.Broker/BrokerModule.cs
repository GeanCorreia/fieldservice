using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FieldService.Broker.Configuration;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Services;

namespace FieldService.Broker;

public static class BrokerModule
{
    public static IServiceCollection AddBrokerModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = BrokerConfiguration.GetRabbitMqConnectionString(configuration);
        var connectionFactory = BrokerConfiguration.CreateConnectionFactory(connectionString);
        var exchanges = configuration.GetSection("RabbitMQ:Topology:Exchanges").Get<List<ExchangeDeclaration>>() ?? [];
        var queues = configuration.GetSection("RabbitMQ:Topology:Queues").Get<List<QueueDeclaration>>() ?? [];
        var bindings = configuration.GetSection("RabbitMQ:Topology:Bindings").Get<List<BindingDeclaration>>() ?? [];

        using (var bootstrapConnection = connectionFactory.CreateConnection())
        using (var bootstrapChannel = bootstrapConnection.CreateModel())
        {
            ExchangeDeclare.Execute(bootstrapChannel, exchanges);
            QueueDeclare.Execute(bootstrapChannel, queues);
            QueueBind.Execute(bootstrapChannel, bindings);
        }

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IRabbitMqPersistentConnection>(_ => new RabbitMqPersistentConnection(connectionFactory));
        services.AddSingleton<IRabbitMqChannelFactory, RabbitMqChannelFactory>();
        services.AddSingleton<IMessageProducer, RabbitMqMessageProducer>();
        services.AddSingleton<IMessageConsumer, RabbitMqMessageConsumer>();

        return services;
    }
}
