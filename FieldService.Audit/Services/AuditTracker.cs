using FieldService.Data.Interfaces;
using FieldService.Shared.Interfaces;
using FieldService.Shared.Types;
using FiledService.Audit.Entities;
using FiledService.Audit.Interfaces;
using FiledService.Audit.Logs;
using Microsoft.Extensions.Logging;

namespace FiledService.Audit.Services;
public class AuditTracker : IAuditTracker
{
    private readonly IRequestContextManager _requestContextManager;
    private readonly IEntityChangeCollector _changeCollector;
    private readonly IAuditChangeRepository _auditChangeRepository;
    private   readonly ILogger<AuditTracker> _logger;
    
    private AuditAccess? _auditAccess = null;

    public AuditTracker(
        IRequestContextManager requestContextManager,
        IEntityChangeCollector changeCollector,
        IAuditChangeRepository auditChangeRepository,
        ILogger<AuditTracker> logger)
    {
        _requestContextManager = requestContextManager ?? throw new ArgumentNullException(nameof(requestContextManager));
        _changeCollector = changeCollector  ?? throw new ArgumentNullException(nameof(changeCollector));
        _auditChangeRepository = auditChangeRepository  ?? throw new ArgumentNullException(
            nameof(auditChangeRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Persist(CancellationToken ct = default)
    {
        var auditChanges = TaskChange();

        if (auditChanges.Any())
        {
            try
            {
                await _auditChangeRepository.Save(auditChanges);
            }

            catch (Exception ex)
            {
                _logger.LogTrackPersist(
                    LogLevel.Error,
                    _requestContextManager.Request.RequestId,
                    ex.Message);
            }
            
        }

        if (_auditAccess != null)
        {
            try
            {
                await _auditChangeRepository.Save(_auditAccess);
            }
            catch (Exception ex)
            {
                _logger.LogTrackPersist(
                    LogLevel.Error,
                    _requestContextManager.Request.RequestId,
                    ex.Message);
            }
        }
    }

    public void TrackAccess(
        string resource, 
        Guid? resourceId,
        IEnumerable<AuditAccessParameter>? parameters)
    {
        try
        {
            _auditAccess = AuditAccess.Create(
                requestId: _requestContextManager.Request.RequestId,
                userId: GetUserId(),
                tenantId: GetTenantId(),
                occurredAt: _requestContextManager.Request.Timestamp,
                resource: resource,
                resourceId: resourceId,
                parameters: parameters);
            
        }catch (Exception ex)
        {
            
            _logger.LogTrackAccess(
                LogLevel.Error,
                _requestContextManager.Request.RequestId,
                resource,
                resourceId?.ToString(),
                ex.Message);

        }
        
    }

    private IEnumerable<AuditChange> TaskChange()
    {
        var collectedChanges = _changeCollector.Consume();
        if (collectedChanges.Count == 0)
            return Enumerable.Empty<AuditChange>();

        var userId = GetUserId();
        var tenantId = GetTenantId();
        var requestContext = _requestContextManager.Request;

        var auditChanges =  new List<AuditChange>();


        foreach (var collected in collectedChanges)
        {
            try
            {
                var auditChange = AuditChange.Create(
                    requestId: requestContext.RequestId,
                    userId: userId,
                    tenantId: tenantId is not null ? tenantId.Value : Guid.Empty,
                    occurredAt: requestContext.Timestamp,
                    resource: collected.Resource,
                    resourceId: collected.ResourceId,
                    changedProperties: collected.Changes);

                auditChanges.Add(auditChange);
            }
            catch (Exception ex)
            {
                _logger.LogTrackChange(
                    LogLevel.Error,
                    requestContext.RequestId,
                    collected.Resource,
                    collected.ResourceId?.ToString(),
                    ex.Message);
            }
            
        }
            
        
        return auditChanges;
    }

    private Guid GetUserId()
    {
        var value = _requestContextManager.Principal.FindFirst(ClaimsExtensions.UserId)?.Value;

        if (value is null)
            throw new InvalidOperationException("UserId claim not found.");

        return Guid.Parse(value);
    }

    private Guid? GetTenantId()
    {
        var value = _requestContextManager.Principal.FindFirst(ClaimsExtensions.TenantId)?.Value;

        if (value is null)
            return null;

        return Guid.Parse(value);
    }
}
