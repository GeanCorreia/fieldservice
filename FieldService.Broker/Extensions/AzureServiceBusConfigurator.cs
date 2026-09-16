using Azure;
using Azure.Messaging.ServiceBus.Administration;
using FieldService.Broker.Interfaces;
using Microsoft.Extensions.Logging;

namespace FieldService.Broker.Extensions;

public class AzureServiceBusConfigurator : IBrokerConfigurator
{
    private const string ConfigurationLockKey = "broker:azure-servicebus:topology:configure";
    private readonly ServiceBusAdministrationClient _adminClient;
    private readonly ILogger<AzureServiceBusConfigurator> _logger;
    private readonly IMessageProcessingLock _lock;

    public AzureServiceBusConfigurator(
        ServiceBusAdministrationClient adminClient,
        ILogger<AzureServiceBusConfigurator> logger,
        IMessageProcessingLock @lock)
    {
        _adminClient = adminClient ?? throw new ArgumentNullException(nameof(adminClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lock = @lock ?? throw new ArgumentNullException(nameof(@lock));
    }

    public async Task Configure(CancellationToken cancellationToken = default)
    {
        var acquired = await _lock.BrokerConfiguratorAcquireLock(cancellationToken);
        if (!acquired)
        {
            _logger.LogInformation(
                "Azure Service Bus topology configuration is already being managed by another application instance. Skipping provisioning.");
            return;
        }

        try
        {
            _logger.LogInformation("Starting Azure Service Bus production topology discovery and provisioning...");

            var publishContexts = BrokerTopologyDiscoverer.DiscoverPublishContexts().ToList();
            var subscribeContexts = BrokerTopologyDiscoverer.DiscoverSubscriptionContexts().ToList();
            var managedTopicNames = publishContexts
                .Select(p => p.EntityName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var publishContext in publishContexts)
            {
                await EnsureTopicExistsAsync(publishContext, cancellationToken);
            }

            foreach (var subscribeContext in subscribeContexts)
            {
                await EnsureSubscriptionExistsAsync(
                    subscribeContext,
                    managedTopicNames.Contains(subscribeContext.EntityName),
                    cancellationToken);
            }

            _logger.LogInformation("Azure Service Bus topology provisioning completed successfully.");
        }
        finally
        {
            await _lock.BrokerConfiguratorReleaseLock(cancellationToken);
        }
    }

    private async Task EnsureTopicExistsAsync(
        Message.BrokerPublishContext publishContext, 
        CancellationToken cancellationToken)
    {
        var topicName = publishContext.EntityName;

        try
        {
            var topicExists = await _adminClient.TopicExistsAsync(topicName, cancellationToken);

            if (!topicExists)
            {
                _logger.LogInformation("Creating Azure Service Bus Topic: {TopicName}", topicName);

                var options = new CreateTopicOptions(topicName)
                {
                    DefaultMessageTimeToLive = publishContext.TimeToLive ?? TimeSpan.FromDays(14),
                    EnablePartitioning = !string.IsNullOrEmpty(publishContext.PartitionKey)
                };

                await _adminClient.CreateTopicAsync(options, cancellationToken);
                _logger.LogInformation("Topic '{TopicName}' created successfully.", topicName);
            }
            else
            {
                _logger.LogDebug("Topic '{TopicName}' already exists.", topicName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision Topic '{TopicName}' in Azure Service Bus.", topicName);
            throw;
        }
    }

    private async Task EnsureSubscriptionExistsAsync(
        Message.BrokerSubscribeContext subscribeContext, 
        bool isManagedTopic,
        CancellationToken cancellationToken)
    {
        var topicName = subscribeContext.EntityName;
        var subName = subscribeContext.SubscriptionName;

        try
        {
            if (!isManagedTopic)
            {
                var topicExists = await _adminClient.TopicExistsAsync(topicName, cancellationToken);
                if (!topicExists)
                {
                    _logger.LogWarning(
                        "Skipping subscription provisioning for external topic '{TopicName}' because it does not exist in the namespace. Subscription: '{SubscriptionName}'.",
                        topicName,
                        subName);
                    return;
                }
            }

            var subExists = await _adminClient.SubscriptionExistsAsync(topicName, subName, cancellationToken);

            if (!subExists)
            {
                _logger.LogInformation(
                    "Creating Azure Service Bus Subscription: '{SubscriptionName}' on Topic: '{TopicName}'", 
                    subName, 
                    topicName);

                var options = new CreateSubscriptionOptions(topicName, subName)
                {
                    MaxDeliveryCount = 10,
                    RequiresSession = subscribeContext.EnableSessions,
                    DefaultMessageTimeToLive = TimeSpan.FromDays(14),
                    LockDuration = subscribeContext.AutoRenewTimeout ?? TimeSpan.FromMinutes(1)
                };

                await _adminClient.CreateSubscriptionAsync(options, cancellationToken);
                _logger.LogInformation("Subscription '{SubscriptionName}' created successfully.", subName);
            }
            else
            {
                _logger.LogDebug("Subscription '{SubscriptionName}' on Topic '{TopicName}' already exists.", subName, topicName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Failed to provision Subscription '{SubscriptionName}' for Topic '{TopicName}' in Azure Service Bus.", 
                subName, 
                topicName);
            throw;
        }
    }
}