using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using FieldService.Queue.Interfaces;

namespace FieldService.Queue.Extensions;

public static class QueueServiceCollectionExtensions
{

    public static IServiceCollection AddQueueConsumers(
        this IServiceCollection services, 
        params Assembly[] assembliesToScan)
    {
        var typedConsumerType = typeof(IQueueConsumer<>);
        var nonTypedConsumerType = typeof(IQueueConsumer);

        var implementations = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(type =>
            {
                var interfaces = type.GetInterfaces();

                var hasNonTypedConsumer = interfaces.Any(i => i == nonTypedConsumerType);
                if (hasNonTypedConsumer)
                    return true;

                return interfaces.Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typedConsumerType);
            });

        foreach (var implementationType in implementations)
        {
            services.AddScoped(implementationType);
        }

        return services;
    }
}