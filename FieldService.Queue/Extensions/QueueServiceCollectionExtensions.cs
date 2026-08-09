using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using FieldService.Queue.Interfaces;

namespace FieldService.Queue.Extensions;

public static class QueueServiceCollectionExtensions
{
    /// <summary>
    /// Escaneia os assemblies fornecidos e registra todos os IJobConsumer no DI como Scoped.
    /// </summary>
    public static IServiceCollection AddQueueConsumers(
        this IServiceCollection services, 
        params Assembly[] assembliesToScan)
    {
        var consumerType = typeof(IQueueConsumer<>);

        var implementations = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces(), (type, implInterface) => new { type, implInterface })
            .Where(i => i.implInterface.IsGenericType && i.implInterface.GetGenericTypeDefinition() == consumerType);

        foreach (var item in implementations)
        {
            // Registra a implementação como Scoped para garantir que dependências como DbContext funcionem corretamente
            services.AddScoped(item.type);
        }

        return services;
    }
}