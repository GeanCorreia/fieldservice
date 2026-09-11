using FieldService.Broker.Channels;
using FieldService.Broker.Entities;
using FieldService.Broker.Interfaces;
using FieldService.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace FieldService.Broker.Services;

public class AzureServiceBusPublisher : IBrokerPublisher
{
    private readonly BrokerMessageChannel _brokerMessageChannel;
    private readonly IBrokerOutboxRepository _repository;
    private readonly ILogger<AzureServiceBusPublisher> _logger;
    private readonly IUnitOfWork _unitOfWork;
    
    public AzureServiceBusPublisher(
        BrokerMessageChannel brokerMessageChannel,
        IUnitOfWork unitOfWork,
        IBrokerOutboxRepository repository,
        ILogger<AzureServiceBusPublisher> logger)
    {
        _brokerMessageChannel = brokerMessageChannel ?? throw new ArgumentNullException(nameof(brokerMessageChannel));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(
        IBrokerEnvelopeMessage brokerEnvelopeEnvelopeMessage, 
        CancellationToken ct = default) 
    {
        ArgumentNullException.ThrowIfNull(brokerEnvelopeEnvelopeMessage);

        var outbox = BrokerOutbox.Create(brokerEnvelopeEnvelopeMessage);

        if (_unitOfWork.HasActiveTransaction)
        {
            await PublishWithExistingTransactionAsync(outbox, ct);
            return;
        }
        
        await PublishWithLocalTransactionAsync(outbox, ct);
        
    }

    public async Task PublishBatchAsync(
        IEnumerable<IBrokerEnvelopeMessage> messages, 
        CancellationToken ct = default) 
    {
        ArgumentNullException.ThrowIfNull(messages);

        var outboxItems = messages.Select(BrokerOutbox.Create).ToList();
        if (outboxItems.Count == 0) return;

        if (_unitOfWork.HasActiveTransaction)
        {
            await PublishBatchWithExistingTransactionAsync(outboxItems, ct);
            return;
        }
        
        await PublishBatchWithLocalTransactionAsync(outboxItems, ct);
        
    }



    private async Task PublishWithExistingTransactionAsync(BrokerOutbox outbox, CancellationToken ct)
    {
        await _repository.SaveAsync(outbox, ct);

        _unitOfWork.OnCommitted(async token =>
        {
            await _brokerMessageChannel.EnqueueAsync(outbox, token);
        });
    }

    private async Task PublishBatchWithExistingTransactionAsync(IReadOnlyCollection<BrokerOutbox> outboxItems, CancellationToken ct)
    {
        _unitOfWork.OnCommitted(async token =>
        {
            foreach (var outbox in outboxItems)
            {
                await _brokerMessageChannel.EnqueueAsync(outbox, token);
            }
        });
        await _repository.SaveAsync(outboxItems, ct);

        
    }
    
    private async Task PublishWithLocalTransactionAsync(BrokerOutbox outbox, CancellationToken ct)
    {
        await _unitOfWork.BeginAsync(ct);
         _unitOfWork.OnCommitted(async token =>
         {
             await _brokerMessageChannel.EnqueueAsync(outbox, token);
         });

        try
        {
            await _repository.SaveAsync(outbox, ct);
            await _unitOfWork.CommitAsync(ct);
            
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    private async Task PublishBatchWithLocalTransactionAsync(
        IReadOnlyCollection<BrokerOutbox> outboxItems, 
        CancellationToken ct)
    {
        await _unitOfWork.BeginAsync(ct);
        
        _unitOfWork.OnCommitted(async token =>
        {
            foreach (var outbox in outboxItems)
            {
                await _brokerMessageChannel.EnqueueAsync(outbox, token);
            }
        });

        try
        {
            await _repository.SaveAsync(outboxItems, ct);
            await _unitOfWork.CommitAsync(ct);
            
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}