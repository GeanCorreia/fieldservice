using Azure.Messaging.ServiceBus;
using FieldService.Broker.Channels;
using FieldService.Broker.Configuration;
using FieldService.Broker.Entities;
using FieldService.Broker.Interfaces;
using FieldService.Broker.Logs;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldService.Broker.Services;

internal class BackgroundBrokerOutboxService : BackgroundService, IBrokerOutboxService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BrokerMessageChannel _channel;
    private readonly ILogger<BackgroundBrokerOutboxService> _logger;
    private readonly IMessageProcessingLock _messageProcessingLock;

    public BackgroundBrokerOutboxService(
        ServiceBusClient serviceBusClient,
        IServiceScopeFactory scopeFactory, 
        BrokerMessageChannel channel,
        ILogger<BackgroundBrokerOutboxService> logger,
        IMessageProcessingLock messageProcessingLock)
    {
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageProcessingLock = messageProcessingLock ?? throw new ArgumentNullException(nameof(messageProcessingLock));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        await foreach (var brokerOutbox in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            var lockAcquired = await _messageProcessingLock.MessageAcquireLock(
                brokerOutbox.MessageId,  
                stoppingToken);

            try
            {
                if (brokerOutbox is null)
                {
                    throw new ArgumentNullException(nameof(brokerOutbox));
                }

                await EnqueueAsync(brokerOutbox, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogOutbox(
                    LogLevel.Error, 
                    "ExecuteAsync", 
                    null, ex.Message);
            }
            finally
            {
                if (lockAcquired)
                {
                    await _messageProcessingLock.MessageReleaseLock(
                        brokerOutbox.MessageId, 
                        stoppingToken);
                }
            }
        }
    }
    
    public async Task EnqueueAsync(
        BrokerOutbox brokerOutbox, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(brokerOutbox);

        var messageId = brokerOutbox.MessageId;
        var serviceBusMessage = brokerOutbox.ServiceBusMessage;
        var entityName = brokerOutbox.ServiceBusMessage.Subject;
        
        try
        {
            await SendMessageAsync(serviceBusMessage, entityName, ct);
        }
        catch (Exception ex)
        {
            _logger.LogSendMessage(
                LogLevel.Error,
                messageId.ToString(),
                entityName,
                ex.Message);
            throw;
        }
        try
        {
            await MarkAsDispatchedAsync(messageId, ct); 
        }
        catch (Exception ex)
        {
            _logger.LogMarkAsDispatched(
                LogLevel.Error,
                messageId.ToString(),
                ex.Message);
            throw;
        }
        
    }

    private async Task SendMessageAsync(ServiceBusMessage message, string entityName, CancellationToken ct)
    {
        await using var sender = _serviceBusClient.CreateSender(entityName);
        await sender.SendMessageAsync(message, ct);
    }
    
    private async Task MarkAsDispatchedAsync(Guid messageId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBrokerOutboxRepository>();

        var outboxMessage = await repository.GetByIdAsync(messageId, ct);
        if (outboxMessage is null)
            throw new InvalidOperationException($"Message with id {messageId} does not exist.");
        
        outboxMessage.MarkAsDispatched(DateTimeOffset.UtcNow);
        await repository.SaveAsync(outboxMessage, ct);
    }

    
}
