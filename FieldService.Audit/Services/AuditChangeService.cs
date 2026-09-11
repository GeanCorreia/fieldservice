using FieldService.Data.Interfaces;
using FiledService.Audit.Entities;
using FiledService.Audit.Interfaces;
using FiledService.Audit.Logs;
using Microsoft.Extensions.Logging;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;

namespace FiledService.Audit.Services;

public sealed class AuditChangeService(
    IEntityChangeCollector changeCollector,
    IAuditChangeRepository auditChangeRepository,
    ILogger<AuditChangeService> logger) : IAuditChangeService
{
    public async Task AuditChanges(CancellationToken ct = default)
    {
        var auditChanges = TrackChanges();

        if (!auditChanges.Any())
            return;

        try
        {
            await auditChangeRepository.Save(auditChanges);
        }
        catch (Exception ex)
        {
            logger.LogTrackPersist(LogLevel.Error, ex.Message);
        }
    }

    private IEnumerable<AuditChange> TrackChanges()
    {
        var collectedChanges = changeCollector.Consume();
        if (collectedChanges.Count == 0)
            return Enumerable.Empty<AuditChange>();

        var auditChanges = new List<AuditChange>();
        foreach (var collected in collectedChanges)
        {
            try
            {
                var auditChange = AuditChange.Create(
                    requestId: GetRequestId(),
                    userId: GetUserId(),
                    tenantId: GetTenantId(),
                    occurredAt: GetOccurredAt(),
                    resource: collected.Resource,
                    resourceId: collected.ResourceId ?? string.Empty,
                    changedProperties: collected.Changes);

                auditChanges.Add(auditChange);
            }
            catch (Exception ex)
            {
                logger.LogTrackChange(
                    LogLevel.Error,
                    collected.Resource,
                    collected.ResourceId?.ToString(),
                    ex.Message);
            }
        }

        return auditChanges;
    }

    private static Guid GetUserId()
    {
        return ObservabilityExecutionContext.UserId
               ?? throw new InvalidOperationException("UserId not found in execution context.");
    }

    private static Guid GetTenantId()
    {
        return ObservabilityExecutionContext.TenantId
               ?? throw new InvalidOperationException("TenantId not found in execution context.");
    }

    private static Guid GetRequestId()
    {
        return ObservabilityExecutionContext.RequestId
               ?? throw new InvalidOperationException("RequestId not found in execution context.");
    }

    private static DateTimeOffset GetOccurredAt()
    {
        return ObservabilityExecutionContext.Timestamp;
    }
}
