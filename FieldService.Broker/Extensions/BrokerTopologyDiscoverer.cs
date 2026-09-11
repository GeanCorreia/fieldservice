using System.Reflection;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;

namespace FieldService.Broker.Extensions;

public class BrokerTopologyDiscoverer : IBrokerTopologyDiscoverer
{
    private static readonly Lazy<Assembly[]> AssembliesLazy = new(LoadAssemblies);

    private static Assembly[] LoadAssemblies()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var baseDirectory = AppContext.BaseDirectory;

        foreach (var assemblyPath in Directory.EnumerateFiles(baseDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var assemblyLocation = Path.GetFullPath(assemblyPath);
                if (loadedAssemblies.Any(a => string.Equals(a.Location, assemblyLocation, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var assembly = Assembly.LoadFrom(assemblyLocation);
                loadedAssemblies.Add(assembly);
            }
            catch
            {
                // Ignored: Runtime-managed assemblies that cannot be loaded from disk.
            }
        }

        return loadedAssemblies
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .DistinctBy(a => a.Location, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null)!;
        }
    }

    public static IEnumerable<BrokerTopology> DiscoverBrokerTopologies()
    {
        var publishContexts = DiscoverPublishContexts();
        var subscribeContexts = DiscoverSubscriptionContexts();

        var subscriptionsByEntity = subscribeContexts
            .GroupBy(s => s.EntityName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => s.SubscriptionName).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var topologies = new List<BrokerTopology>();
        foreach (var publishContext in publishContexts)
        {
            var entityName = publishContext.EntityName;
            var subscriptions = subscriptionsByEntity.TryGetValue(entityName, out var subs)
                ? subs
                : Enumerable.Empty<string>();

            topologies.Add(new BrokerTopology(
                EntityName: entityName,
                Subscriptions: subscriptions
            ));
        }

        return topologies;
    }

    public static IEnumerable<BrokerPublishContext> DiscoverPublishContexts()
    {
        var publishContexts = new List<BrokerPublishContext>();
        var knownEntityNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var producerTypes = AssembliesLazy.Value
            .SelectMany(GetLoadableTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(IsProducerType);

        foreach (var producerType in producerTypes)
        {
            var contextProperty = producerType.GetProperty("BrokerPublishContext", BindingFlags.Public | BindingFlags.Static);
            
            if (contextProperty is null)
            {
                throw new InvalidOperationException(
                    $"Producer '{producerType.FullName}' implements 'IBrokerProducer' but does not declare a public static 'BrokerPublishContext' property.");
            }

            if (contextProperty.GetValue(null) is not BrokerPublishContext publishContext)
            {
                throw new InvalidOperationException(
                    $"Producer '{producerType.FullName}' defined 'BrokerPublishContext' property with a null value.");
            }

            if (string.IsNullOrWhiteSpace(publishContext.EntityName))
            {
                throw new InvalidOperationException(
                    $"Producer '{producerType.FullName}' defined a 'BrokerPublishContext' with a null or empty EntityName.");
            }
            
            if (!knownEntityNames.Add(publishContext.EntityName))
            {
                throw new InvalidOperationException(
                    $"Broker topology collision detected: EntityName '{publishContext.EntityName}' is declared by multiple producers. " +
                    $"Duplicated producer type: '{producerType.FullName}'.");
            }

            publishContexts.Add(publishContext);
        }

        return publishContexts;
    }

    public static IEnumerable<BrokerSubscribeContext> DiscoverSubscriptionContexts()
    {
        var subscribeContexts = new List<BrokerSubscribeContext>();

        var validPublishContexts = DiscoverPublishContexts();
        var validEntityNames = new HashSet<string>(
            validPublishContexts.Select(p => p.EntityName),
            StringComparer.OrdinalIgnoreCase
        );

        var registeredSubscriptions = new HashSet<(string EntityName, string SubscriptionName)>(
            EqualityComparer<(string EntityName, string SubscriptionName)>.Create(
                (x, y) => string.Equals(x.EntityName, y.EntityName, StringComparison.OrdinalIgnoreCase) &&
                          string.Equals(x.SubscriptionName, y.SubscriptionName, StringComparison.OrdinalIgnoreCase),
                obj => HashCode.Combine(
                    obj.EntityName?.ToUpperInvariant(),
                    obj.SubscriptionName?.ToUpperInvariant())
            )
        );

        var consumerTypes = AssembliesLazy.Value
            .SelectMany(GetLoadableTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(IsConsumerType);

        foreach (var consumerType in consumerTypes)
        {
            var contextProperty = consumerType.GetProperty("BrokerSubscriptionContext", BindingFlags.Public | BindingFlags.Static)
                ?? consumerType.GetProperty("SubscriptionContext", BindingFlags.Public | BindingFlags.Static);

            if (contextProperty is null)
            {
                throw new InvalidOperationException(
                    $"Consumer '{consumerType.FullName}' implements consumer interface but does not declare a public static 'SubscriptionContext' property.");
            }

            if (contextProperty.GetValue(null) is not BrokerSubscribeContext subscribeContext)
            {
                throw new InvalidOperationException(
                    $"Consumer '{consumerType.FullName}' defined 'SubscriptionContext' property with a null value.");
            }

            if (string.IsNullOrWhiteSpace(subscribeContext.EntityName))
            {
                throw new InvalidOperationException(
                    $"Consumer '{consumerType.FullName}' defined a 'BrokerSubscribeContext' with a null or empty EntityName.");
            }

            if (string.IsNullOrWhiteSpace(subscribeContext.SubscriptionName))
            {
                throw new InvalidOperationException(
                    $"Consumer '{consumerType.FullName}' defined a 'BrokerSubscribeContext' with a null or empty SubscriptionName.");
            }
            
            if (!validEntityNames.Contains(subscribeContext.EntityName))
            {
                throw new InvalidOperationException(
                    $"Broker topology orphan consumer detected: Consumer '{consumerType.FullName}' tried to subscribe to EntityName '{subscribeContext.EntityName}', " +
                    $"but no registered producer exists for this topic.");
            }
            
            var subscriptionPair = (subscribeContext.EntityName, subscribeContext.SubscriptionName);
            if (!registeredSubscriptions.Add(subscriptionPair))
            {
                throw new InvalidOperationException(
                    $"Broker topology collision detected: SubscriptionName '{subscribeContext.SubscriptionName}' " +
                    $"for EntityName '{subscribeContext.EntityName}' is declared by multiple consumers. " +
                    $"Duplicated consumer type: '{consumerType.FullName}'.");
            }

            subscribeContexts.Add(subscribeContext);
        }

        return subscribeContexts;
    }

    private static bool IsProducerType(Type type)
    {
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBrokerProducer<>));
    }

    private static bool IsConsumerType(Type type)
    {
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && (
                i.GetGenericTypeDefinition() == typeof(IBrokerConsumer<>) ||
                i.GetGenericTypeDefinition() == typeof(IBrokerBatchConsumer<>)
            ));
    }
}