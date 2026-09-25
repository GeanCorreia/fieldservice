using FieldService.Data.Interfaces;
using FieldService.Audit.Channels;
using FiledService.Audit.Entities;
using FiledService.Audit.Interfaces;
using FiledService.Audit.Logs;
using Microsoft.Extensions.Logging;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;

namespace FiledService.Audit.Services;

public sealed class AuditChangeService(
    IEntityChangeCollector changeCollector,
    AuditChangeChannel channel,
    ILogger<AuditChangeService> logger) : IAuditChangeService
{
    public async Task AuditChanges(CancellationToken ct = default)
    {
        var auditChanges = TrackChanges().ToList();

        if (auditChanges.Count == 0)
            return;
        
        try
        {
            foreach (var auditChange in auditChanges)
            {
                await channel.EnqueueAsync(auditChange, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogTrackPersist(LogLevel.Error, ex.Message);
        }
    }

    private IEnumerable<AuditChange> TrackChanges()
    {

        if (!ObservabilityExecutionContext.UserId.HasValue || !ObservabilityExecutionContext.TenantId.HasValue)
            return Enumerable.Empty<AuditChange>();

        var collectedChanges = changeCollector.Consume();
        if (collectedChanges.Count == 0)
            return Enumerable.Empty<AuditChange>();

        var userId = ObservabilityExecutionContext.UserId.Value;
        var tenantId = ObservabilityExecutionContext.TenantId.Value;
        var requestId = ObservabilityExecutionContext.RequestId ?? throw new InvalidOperationException("RequestId is not available in the execution context.");
        var occurredAt = ObservabilityExecutionContext.Timestamp;

        var auditChanges = new List<AuditChange>(collectedChanges.Count);

        foreach (var collected in collectedChanges)
        {
            try
            {
                var auditChange = AuditChange.Create(
                    requestId: requestId,
                    userId: userId,
                    tenantId: tenantId,
                    occurredAt: occurredAt,
                    resource: collected.Resource,
                    resourceId: collected.ResourceId,
                    changedProperties: collected.Changes);

                auditChanges.Add(auditChange);
            }
            catch (Exception ex)
            {
                logger.LogTrackChange(
                    LogLevel.Error,
                    collected.Resource,
                    collected.ResourceId,
                    ex.Message);
            }
        }

        return auditChanges;
    }
}