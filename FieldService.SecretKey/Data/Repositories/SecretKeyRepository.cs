using FieldService.Data.Interfaces;
using FieldService.SecretKey.Events;
using FieldService.SecretKey.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.SecretKey.Data.Repositories;

internal class SecretKeyRepository(
    SecretKeyDbContext dbContext,
    ISqlUnitOfWork<SecretKeyDbContext> sqlUnitOfWork
    ) : ISecretKeyRepository
{
    
    private static void ValidateId(Guid id, string paramName)
    {
        if (id == default)
            throw new ArgumentException("Id cannot be empty.", paramName);
    }
    public async Task<Entities.SecretKey?> GetSecretKeyAsync(
        Guid tenantId,
        string name, 
        CancellationToken cancellationToken = default)
    {
        ValidateId(tenantId, nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        
        var sanitizedSearchName = Entities.SecretKey.SanitizeName(name);
        
        var targetSecretIdQuery = dbContext.SecretKeyEvents
            .Where(e => e.Name == sanitizedSearchName 
                        && (e.EventType == SecretKeyEventType.Created || e.EventType == SecretKeyEventType.UpdatedName))
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => e.SecretKeyId);
        
        var secretKey = await dbContext.SecretKeys
            .Include(x => x.SecretKeyEvents) 
            .Where(x => x.TenantId == tenantId && targetSecretIdQuery.Contains(x.Id))
            .FirstOrDefaultAsync(cancellationToken);
        
        if (secretKey != null && secretKey.IsDeleted)
        {
            return null; 
        }

        return secretKey;
    }

    public async Task<Entities.SecretKey?> GetSecretKeyAsync(
        Guid secretKeyId, 
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SecretKeys
            .Include(x => x.SecretKeyEvents)
            .FirstOrDefaultAsync(x => x.Id == secretKeyId, cancellationToken);
    }

    public async Task SaveSecretKeyAsync(Entities.SecretKey secretKey, CancellationToken cancellationToken = default)
    {
        var trackedSecret = await dbContext.SecretKeys
            .Include(x => x.SecretKeyEvents)
            .FirstOrDefaultAsync(x => x.Id == secretKey.Id, cancellationToken);

        if (trackedSecret == null)
        {
            await dbContext.SecretKeys.AddAsync(secretKey, cancellationToken);
        }
        else
        {
            dbContext.Entry(trackedSecret).CurrentValues.SetValues(secretKey);
            
            var existingEventIds = trackedSecret.SecretKeyEvents.Select(e => e.Id).ToHashSet();
        
            var newEvents = secretKey.SecretKeyEvents
                .Where(e => !existingEventIds.Contains(e.Id));
            
            foreach (var newEvent in newEvents)
            {
                dbContext.Set<SecretKeyEvent>().Add(newEvent);
            }
        }

        await sqlUnitOfWork.PersistChangesAsync(cancellationToken);
    }
}