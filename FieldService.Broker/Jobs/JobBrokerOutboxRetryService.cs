using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FieldService.Broker.Configuration;
using FieldService.Broker.Entities;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Logs;
using FieldService.Broker.Mappers;
using FieldService.Broker.Message;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;
using FieldService.Shared.Message;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldService.Broker.Jobs;

public sealed class JobBrokerOutboxRetryService : IQueueConsumer, IBrokerOutboxRetryService
{
    private readonly IBrokerOutboxRepository _repository;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<JobBrokerOutboxRetryService> _logger;
    private readonly BrokerOutboxOptions _options;
    private readonly IMessageProcessingLock _messageProcessingLock;

    public JobBrokerOutboxRetryService(
        IBrokerOutboxRepository repository,
        ServiceBusClient serviceBusClient,
        IOptions<BrokerOutboxOptions> options,
        ILogger<JobBrokerOutboxRetryService> logger,
        IMessageProcessingLock messageProcessingLock)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageProcessingLock = messageProcessingLock ?? throw new ArgumentNullException(nameof(messageProcessingLock));
    }

    public async Task ExecuteAsync(Job job, CancellationToken ct = default)
    {
        if (job.Type != BrokerOutboxRetryJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Type}'.");

        var interval = TimeSpan.FromMinutes(_options.PendingRetryIntervalMinutes);
        var maxCount = _options.MaxRetryCount;
        var brokerOutboxes = await _repository.GetPendingAsync(
            interval,
            maxCount,
            ct);

        await OutboxAsync(brokerOutboxes, ct);
    }

    public async Task OutboxAsync(
        IEnumerable<BrokerOutbox> messages,
        CancellationToken ct = default)
    {
        var brokerOutboxes = messages.ToList();
        if (brokerOutboxes.Count == 0)
            return;

        var messagesByEntity = new Dictionary<string, List<ServiceBusMessage>>();
        var outboxesByEntity = new Dictionary<string, List<BrokerOutbox>>();
        
        

        foreach (var brokerOutbox in brokerOutboxes)
        {
            var lockAcquired = await _messageProcessingLock.MessageAcquireLock(brokerOutbox.MessageId, ct);
            
            try
            {
                
                if(!lockAcquired)
                    continue;
                
                var entity = brokerOutbox.ServiceBusMessage.Subject;

                if (!messagesByEntity.TryGetValue(entity, out var validOutboxes))
                {
                    validOutboxes = new List<ServiceBusMessage>();
                    messagesByEntity[entity] = validOutboxes;
                }

                validOutboxes.Add(brokerOutbox.ServiceBusMessage);

                if (!outboxesByEntity.TryGetValue(entity, out var validOutboxesList))
                {
                    validOutboxesList = new List<BrokerOutbox>();
                    outboxesByEntity[entity] = validOutboxesList;
                }

                validOutboxesList.Add(brokerOutbox);
                
            }
            catch (Exception ex)
            {
                if(lockAcquired)
                    await _messageProcessingLock.MessageReleaseLock(brokerOutbox.MessageId, ct);
                    
                _logger.LogOutbox(
                    LogLevel.Error,
                    nameof(OutboxAsync),
                    brokerOutbox.MessageId.ToString(),
                    ex.Message);
            }
        }

        if (messagesByEntity.Count == 0)
            return;

        foreach (var (entity, envelopeMessages) in messagesByEntity)
        {
            await EnqueueForTypeAsync(
                entity,
                messagesByEntity[entity],
                outboxesByEntity[entity],
                ct);
        }
    }

    private async Task EnqueueForTypeAsync(
        string entityName,
        IEnumerable<ServiceBusMessage> messages,
        IEnumerable<BrokerOutbox> outboxes,
        CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        var outboxList = outboxes.ToList();

        if (messageList.Count == 0)
        {
            throw new InvalidOperationException("No broker outboxes were provided.");
        }

        try
        {
            try
            {
                await PublishAsync(
                    entityName,
                    messageList,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogBatchSendMessage(
                    LogLevel.Error,
                    outboxList.Select(x => x.MessageId),
                    ex.Message);
                throw;
            }

            try
            {
                await MarkAsDispatchedAsync(outboxList, ct);
            }
            catch (Exception ex)
            {
                _logger.LogBatchMarkAsExpired(
                    LogLevel.Error,
                    outboxList.Select(x => x.MessageId),
                    ex.Message);
                throw;
            }
        }
        finally
        {
            await _messageProcessingLock.MessageReleaseLocks(outboxList.Select(x => x.MessageId), ct);
        }
        
    }

    private async Task PublishAsync(
        string entityName,
        IEnumerable<ServiceBusMessage> messages,
        CancellationToken ct = default)
    {
        
        var messageList = messages.ToList();
        if (messageList.Count == 0)
        {
            return;
        }

       

        await using var sender = _serviceBusClient.CreateSender(entityName);
        var batch = await sender.CreateMessageBatchAsync(ct);

        foreach (var serviceBusMessage in messageList)
        {
            if (batch.TryAddMessage(serviceBusMessage))
                continue;

            if (batch.Count == 0)
                throw new InvalidOperationException($"A single message of payload exceeds the maximum batch size.");

            await sender.SendMessagesAsync(batch, ct);
            batch = await sender.CreateMessageBatchAsync(ct);

            if (!batch.TryAddMessage(serviceBusMessage))
                throw new InvalidOperationException($"A single message of payload exceeds the maximum batch size.");
        }

        if (batch.Count > 0)
            await sender.SendMessagesAsync(batch, ct);
    }

    private async Task MarkAsDispatchedAsync(
        IEnumerable<BrokerOutbox> brokerOutboxes, 
        CancellationToken ct = default)
    {
        if (!brokerOutboxes.Any())
        {
            return;
        }

        var dispatchedAt = DateTimeOffset.UtcNow;
        foreach (var brokerOutbox in brokerOutboxes)
        {
            brokerOutbox.MarkAsDispatched(dispatchedAt);
        }

        await _repository.SaveAsync(brokerOutboxes, ct);
    }
}
