using System.Reflection;
using FieldService.Queue.Producers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            .Where(t => IsRecurringProducer(t));

        foreach (var producerType in producerTypes)
            services.AddScoped(producerType);

        services.AddSingleton(new QueueRecurringProducerRegistry(producerTypes.ToArray()));
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

internal sealed class QueueRecurringHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    private readonly QueueRecurringProducerRegistry _registry = scopeFactory.CreateScope().ServiceProvider
        .GetRequiredService<QueueRecurringProducerRegistry>();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        foreach (var producerType in _registry.ProducerTypes)
        {
            var producer = scope.ServiceProvider.GetRequiredService(producerType);
            var scheduleMethod = producerType.GetMethod("ScheduleRecurring", BindingFlags.Instance | BindingFlags.Public);
            scheduleMethod?.Invoke(producer, Array.Empty<object>());
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class QueueRecurringProducerRegistry(Type[] producerTypes)
{
    public Type[] ProducerTypes { get; } = producerTypes;
}
