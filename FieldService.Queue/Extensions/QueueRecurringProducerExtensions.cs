using System.Reflection;
using FieldService.Queue.Producers;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FieldService.Queue.Extensions;

public static class QueueRecurringProducerExtensions
{
    public static IServiceCollection AddQueueRecurringProducers(
        this IServiceCollection services,
        params Assembly[] assembliesToScan)
    {
        var producerTypes = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => IsRecurringProducer(t))
            .ToArray();

        foreach (var producerType in producerTypes)
            services.AddScoped(producerType);

        services.AddSingleton(new QueueRecurringProducerRegistry(producerTypes));
        services.AddHostedService<QueueRecurringHostedService>();
        return services;
    }

    private static bool IsRecurringProducer(Type type)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AbstractScheduleRecurringProducer<>))
                return true;

            current = current.BaseType;
        }

        return false;
    }
}

internal sealed class QueueRecurringHostedService(
    IServiceScopeFactory scopeFactory,
    QueueRecurringProducerRegistry registry) : IHostedService
{
    private readonly QueueRecurringProducerRegistry _registry = registry;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueueRecurringHostedService>>();

        foreach (var producerType in _registry.ProducerTypes)
        {
            try
            {
                var producer = scope.ServiceProvider.GetRequiredService(producerType);
                var scheduleMethod = producerType.GetMethod("ScheduleRecurring", BindingFlags.Instance | BindingFlags.Public);
                scheduleMethod?.Invoke(producer, Array.Empty<object>());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error scheduling recurring job for producer {ProducerType}", producerType.Name);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    
}

internal sealed class QueueRecurringProducerRegistry(Type[] producerTypes)
{
    public Type[] ProducerTypes { get; } = producerTypes;
}
