using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using FieldService.Broker.Channels;
using FieldService.Broker.Configuration;
using FieldService.Broker.Data;
using FieldService.Broker.Data.Repositories;
using FieldService.Broker.Extensions;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Jobs;
using FieldService.Broker.Services;
using FieldService.Broker.Workers;
using FieldService.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FieldService.Broker;

public static class BrokerModule
{
    public static IServiceCollection AddBrokerModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        
        services.AddSingleton<BrokerMessageChannel>();
        services.Configure<AzureServiceBusOptions>(configuration.GetSection(AzureServiceBusOptions.SectionName));
        services.Configure<BrokerOutboxOptions>(configuration.GetSection(BrokerOutboxOptions.SectionName));

        var azureServiceBusOptions = configuration.GetSection(AzureServiceBusOptions.SectionName).Get<AzureServiceBusOptions>()
            ?? throw new InvalidOperationException("AzureServiceBus options are not configured.");

        if (string.IsNullOrWhiteSpace(azureServiceBusOptions.ConnectionString))
            throw new InvalidOperationException("AzureServiceBus:ConnectionString is not configured.");
        
        services.AddSqlModule<BrokerDbContext>(configuration, environment);

        services.AddSingleton(_ => new ServiceBusClient(azureServiceBusOptions.ConnectionString));
        services.AddSingleton(_ => new ServiceBusAdministrationClient(azureServiceBusOptions.ConnectionString));
        
        services.TryAddSingleton<IMessageProcessingLock, MessageProcessingLock>();
        services.AddScoped<IBrokerPublisher, AzureServiceBusPublisher>();
        services.AddSingleton<IBrokerDispatcher, AzureServiceBusDispatcher>();
        services.AddScoped<IBrokerOutboxRepository, BrokerOutboxRepository>();
        services.AddScoped<IBrokerOutboxRetryService, JobBrokerOutboxRetryService>();
        services.AddSingleton<IBrokerConfigurator, AzureServiceBusConfigurator>();
        
        services.AddHostedService<BackgroundBrokerOutboxService>();
        services.AddHostedService<BrokerDispatcherWorker>();
        services.AddBrokerConsumers();
        services.AddBrokerProducers();
        
        if (!environment.IsDevelopment())
        {
            services.AddHostedService<AzureServiceBusStartupConfigurator>();
        }

        return services;
    }
}
