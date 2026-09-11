using FieldService.Shared.Types;
using FiledService.Audit.Entities;
using FiledService.Audit.Interfaces;
using FiledService.Audit.Logs;
using Microsoft.Extensions.Logging;
using ObservabilityExecutionContext = FieldService.Observability.Services.ExecutionContext;

namespace FiledService.Audit.Services;

public sealed class AuditAccessService(
    IAuditChangeRepository auditChangeRepository,
    ILogger<AuditAccessService> logger) : IAuditAccessService
{
    public async Task AuditAccess(
        string resourceName,
        SchemaVersion schemaVersion,
        Guid? resourceId,
        IEnumerable<AuditAccessParameter>? parameters,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(resourceName);

        Guid? tenantId = null;

        try
        {
            tenantId = GetTenantId();
        }
        catch
        {
        }

        try
        {
            var access = FiledService.Audit.Entities.AuditAccess.Create(
                requestId: GetRequestId(),
                userId: GetUserId(),
                tenantId: tenantId,
                occurredAt: GetOccurredAt(),
                resourceName: resourceName,
                schemaVersion: schemaVersion,
                resourceId: resourceId,
                parameters: parameters);

            await auditChangeRepository.Save(access);
        }
        catch (Exception ex)
        {
            logger.LogTrackAccess(
                LogLevel.Error,
                resourceName,
                resourceId?.ToString(),
                ex.Message);
        }
    }

    private static Guid GetUserId()
    {
        return ObservabilityExecutionContext.UserId
               ?? throw new InvalidOperationException("UserId not found in execution context.");
    }

    private static Guid? GetTenantId()
    {
        return ObservabilityExecutionContext.TenantId;
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
