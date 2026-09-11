using System.Reflection;
using FieldService.Broker.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Broker.Extensions;

public static class BrokerExtensions
{
    public static IServiceCollection AddBrokerConsumers(
        this IServiceCollection services, 
        params Assembly[] assembliesToScan)
    {
        var assemblies = GetAssembliesToScan(assembliesToScan);

        var consumerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i => 
                i.IsGenericType && (
                    i.GetGenericTypeDefinition() == typeof(IBrokerConsumer<>) ||
                    i.GetGenericTypeDefinition() == typeof(IBrokerBatchConsumer<>)
                )))
            .ToList();

        foreach (var consumerType in consumerTypes)
        {
            services.AddScoped(consumerType);
        }

        return services;
    }

    public static IServiceCollection AddBrokerProducers(
        this IServiceCollection services, 
        params Assembly[] assembliesToScan)
    {
        var assemblies = GetAssembliesToScan(assembliesToScan);
        
        var producerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i => 
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBrokerProducer<>)))
            .ToList();

        foreach (var producerType in producerTypes)
        {
            var interfaceType = producerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBrokerProducer<>));
            
            services.AddScoped(interfaceType, producerType);
            services.AddScoped(producerType);
        }

        return services;
    }

    private static Assembly[] GetAssembliesToScan(Assembly[] assembliesToScan)
    {
        return assembliesToScan.Length > 0 
            ? assembliesToScan 
            : AppDomain.CurrentDomain.GetAssemblies();
    }
}