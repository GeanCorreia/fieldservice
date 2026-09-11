using System.Reflection;
using FieldService.Queue.Producers;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Queue.Extensions;

public static class QueueProducerRegistrationExtensions
{
    public static IServiceCollection AddQueueProducers(
        this IServiceCollection services,
        params Assembly[] assembliesToScan)
    {
        var producerTypes = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => IsDerivedFrom(t, typeof(AbstractPublishProducer<>))
                        || IsDerivedFrom(t, typeof(AbstractPublishProducer<,>))
                        || IsDerivedFrom(t, typeof(AbstractPublishDelayedProducer<>))
                        || IsDerivedFrom(t, typeof(AbstractPublishDelayedProducer<,>)));

        foreach (var producerType in producerTypes)
            services.AddScoped(producerType);

        return services;
    }

    private static bool IsDerivedFrom(Type type, Type openGenericBaseType)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGenericBaseType)
                return true;

            current = current.BaseType;
        }

        return false;
    }
}
