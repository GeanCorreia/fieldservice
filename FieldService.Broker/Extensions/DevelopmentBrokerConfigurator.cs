using FieldService.Broker.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Xml;

namespace FieldService.Broker.Extensions;

public static class DevelopmentBrokerConfigurator 
{
    private const string DefaultLocalConfigFileName = "Config.json";
    private const string DefaultLocalConfigDirectory = "docker/ServiceBusEmulator";
    private const string DockerContainerConfigPath = "/ServiceBus_Emulator/ConfigFiles/Config.json";
    
    public static async Task Configure(ILogger? logger = null, string namespaceName = "sbemulatorns")
    {
        var publishContexts = BrokerTopologyDiscoverer.DiscoverPublishContexts();
        var subscribeContexts = BrokerTopologyDiscoverer.DiscoverSubscriptionContexts();
        var subscriptionsByEntity = subscribeContexts
            .GroupBy(s => s.EntityName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => s.SubscriptionName).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var publishContext in publishContexts)
        {
            var subscriptions = subscriptionsByEntity.TryGetValue(publishContext.EntityName, out var list)
                ? list
                : new List<string>();

            Console.WriteLine($"Topic: {publishContext.EntityName}");
            foreach (var subscriptionName in subscriptions)
            {
                Console.WriteLine($"  Subscription: {subscriptionName}");
            }
        }

        var topics = publishContexts
            .Select(publishContext => new
            {
                Name = publishContext.EntityName,
                Properties = BuildTopicProperties(publishContext),
                Subscriptions = (subscriptionsByEntity.TryGetValue(publishContext.EntityName, out var subscriptionsForEntity)
                        ? subscriptionsForEntity
                        : Enumerable.Empty<string>())
                    .Select(subscriptionName =>
                    {
                        var subscriptionContext = subscribeContexts.First(s =>
                            string.Equals(s.EntityName, publishContext.EntityName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(s.SubscriptionName, subscriptionName, StringComparison.OrdinalIgnoreCase));
                        
                        return new
                        {
                            Name = subscriptionName,
                            Properties = BuildSubscriptionProperties(subscriptionContext)
                        };
                    })
                    .ToList()
            })
            .ToList();

        var config = new
        {
            UserConfig = new
            {
                Namespaces = new[]
                {
                    new
                    {
                        Name = namespaceName,
                        Topics = topics,
                        Queues = Array.Empty<object>()
                    }
                },
                Logging = new
                {
                    Type = "File"
                }
            }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        var targetPaths = GetTargetConfigPaths();

        foreach (var targetPath in targetPaths)
        {
            var directoryPath = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            try
            {
                await File.WriteAllTextAsync(targetPath, $"{json}{Environment.NewLine}");
                logger?.LogInformation("Service Bus emulator config file written to: {TargetPath}", targetPath);
            }
            catch (IOException ex)
            {
                logger?.LogWarning(ex, "It was not possible to write Service Bus config to: {TargetPath}. This path may be read-only on the host machine.", targetPath);
            }
        }
    }

    private static IReadOnlyList<string> GetTargetConfigPaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var preferredLocalPath = ResolveLocalConfigPath();
        if (!string.IsNullOrWhiteSpace(preferredLocalPath))
        {
            paths.Add(preferredLocalPath);
        }

        var dockerPath = Environment.GetEnvironmentVariable("SERVICEBUS_EMULATOR_CONFIG_PATH")
            ?? Environment.GetEnvironmentVariable("CONFIG_PATH");

        if (!string.IsNullOrWhiteSpace(dockerPath) && !string.Equals(dockerPath, preferredLocalPath, StringComparison.OrdinalIgnoreCase))
        {
            paths.Add(dockerPath);
        }

        return paths.ToList();
    }

    private static string ResolveLocalConfigPath()
    {
        var env = Environment.GetEnvironmentVariable("FIELD_SERVICE_SERVICEBUS_CONFIG_PATH")
            ?? Environment.GetEnvironmentVariable("SERVICEBUS_EMULATOR_HOST_CONFIG_PATH");

        if (!string.IsNullOrWhiteSpace(env))
        {
            return env;
        }

        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory is not null)
        {
            var dockerComposePath = Path.Combine(currentDirectory.FullName, "docker-compose.yml");
            if (File.Exists(dockerComposePath))
            {
                return Path.Combine(currentDirectory.FullName, DefaultLocalConfigDirectory, DefaultLocalConfigFileName);
            }

            currentDirectory = currentDirectory.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), DefaultLocalConfigDirectory, DefaultLocalConfigFileName);
    }

    private static Dictionary<string, object> BuildTopicProperties(Broker.Message.BrokerPublishContext publishContext)
    {
        var properties = new Dictionary<string, object>
        {
            ["DefaultMessageTimeToLive"] = FormatDurationIso8601(publishContext.TimeToLive ?? TimeSpan.FromDays(14))
        };

        if (!string.IsNullOrWhiteSpace(publishContext.PartitionKey))
        {
            properties["EnablePartitioning"] = true;
        }

        return properties;
    }

    private static Dictionary<string, object> BuildSubscriptionProperties(Broker.Message.BrokerSubscribeContext subscribeContext)
    {
        var properties = new Dictionary<string, object>
        {
            ["DeadLetteringOnMessageExpiration"] = true,
            ["LockDuration"] = FormatDurationIso8601(subscribeContext.AutoRenewTimeout ?? TimeSpan.FromMinutes(1)),
            ["MaxDeliveryCount"] = 10,
            ["RequiresSession"] = subscribeContext.EnableSessions
        };

        return properties;
    }

    private static string FormatDurationIso8601(TimeSpan timeSpan)
    {
        return XmlConvert.ToString(timeSpan);
    }
}