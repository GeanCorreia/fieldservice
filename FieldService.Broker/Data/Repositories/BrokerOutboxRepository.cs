using FieldService.Broker.Entities;
using FieldService.Broker.Interfaces;
using FieldService.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Broker.Data.Repositories;

internal sealed class BrokerOutboxRepository(
    BrokerDbContext dbContext,
    ISqlUnitOfWork<BrokerDbContext> unitOfWork) : IBrokerOutboxRepository
{
    public async Task<BrokerOutbox?> GetByIdAsync(
        Guid messageId, 
        CancellationToken ct = default)
    {
        if (messageId == default)
            throw new ArgumentException("MessageId is required.", nameof(messageId));
    
        return await dbContext.BrokerOutboxes
            .FirstOrDefaultAsync(x => x.MessageId == messageId, ct);
        
    }

    public async Task<IEnumerable<BrokerOutbox>> GetPendingAsync(
        TimeSpan interval,
        int? maxCount = null,
        CancellationToken ct = default)
    {
        if (interval < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval cannot be negative.");
        if (maxCount is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCount), "MaxCount must be greater than zero.");

        var threshold = DateTimeOffset.UtcNow - interval;
        var now = DateTimeOffset.UtcNow;

        var query = dbContext.BrokerOutboxes
            .Where(x => x.DispatchedAt == null &&
                        x.CreatedAt <= threshold &&
                        (x.ExpiresAt == null || now < x.ExpiresAt))
            .OrderBy(x => x.CreatedAt)
            .AsQueryable();

        if (maxCount.HasValue)
        {
            query = query.Take(maxCount.Value);
        }

        return await query.ToListAsync(ct);
    }

    public async Task SaveAsync(BrokerOutbox brokerOutbox, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(brokerOutbox);

        var existing = await dbContext.BrokerOutboxes
            .FirstOrDefaultAsync(x => x.MessageId == brokerOutbox.MessageId, ct);

        if (existing is null)
        {
            await dbContext.BrokerOutboxes.AddAsync(brokerOutbox, ct);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(brokerOutbox);
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task SaveAsync(IEnumerable<BrokerOutbox> brokerOutboxes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(brokerOutboxes);

        var brokerOutboxList = brokerOutboxes.ToList();
        if (brokerOutboxList.Count == 0)
            return;

        var messageIds = brokerOutboxList.Select(x => x.MessageId).ToList();
        var existingBrokerOutboxes = await dbContext.BrokerOutboxes
            .Where(x => messageIds.Contains(x.MessageId))
            .ToListAsync(ct);

        foreach (var brokerOutbox in brokerOutboxList)
        {
            var existing = existingBrokerOutboxes.FirstOrDefault(x => x.MessageId == brokerOutbox.MessageId);

            if (existing is null)
            {
                await dbContext.BrokerOutboxes.AddAsync(brokerOutbox, ct);
            }
            else
            {
                dbContext.Entry(existing).CurrentValues.SetValues(brokerOutbox);
            }
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

   

    public async Task DeleteAsync(
        IEnumerable<Guid> messageIds, 
        CancellationToken ct = default)
    {
        if (!messageIds.Any())
        {
            return;
        }
        
        var brokerOutboxesToDelete = await dbContext.BrokerOutboxes
            .Where(x => messageIds.Contains(x.MessageId))
            .ToListAsync(ct);

        if (brokerOutboxesToDelete.Any())
        {
            dbContext.BrokerOutboxes.RemoveRange(brokerOutboxesToDelete);
            await unitOfWork.PersistChangesAsync(ct);
        }
    }

    public async Task DeleteProcessedBeforeAsync(
        DateTimeOffset threshold, 
        CancellationToken ct = default)
    {
        var dispatchedBrokerOutboxesToDelete = await dbContext.BrokerOutboxes
            .Where(x => x.DispatchedAt != null && x.DispatchedAt < threshold)
            .ToListAsync(ct);
        
        var expiredBrokerOutboxesToDelete = await dbContext.BrokerOutboxes
            .Where(x => x.ExpiresAt != null && x.ExpiresAt < threshold)
            .ToListAsync(ct);
        
        var brokerOutboxesToDelete = new HashSet<BrokerOutbox>();
        brokerOutboxesToDelete.UnionWith(dispatchedBrokerOutboxesToDelete);
        brokerOutboxesToDelete.UnionWith(expiredBrokerOutboxesToDelete);
        
        if (brokerOutboxesToDelete.Any())
        {
            dbContext.BrokerOutboxes.RemoveRange(brokerOutboxesToDelete);
            await unitOfWork.PersistChangesAsync(ct);
        }
    }
}
