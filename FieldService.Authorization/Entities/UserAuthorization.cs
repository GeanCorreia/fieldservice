using FieldService.Authentication.Events;
using FieldService.Authorization.Events;
using FieldService.Authorization.Types;
using FieldService.Shared.Types;

namespace FieldService.Authorization.Entities;

public class UserAuthorization
{
    
    public Guid UserId => _tenantEvents.FirstOrDefault()?.UserId ??
                          _roleEvents.FirstOrDefault()?.UserId ??
                          _permissionEvents.FirstOrDefault()?.UserId ??
                          throw new InvalidOperationException("UserId could not be determined from events.");
    
    public Guid TenantId => _tenantEvents.FirstOrDefault()?.TenantId ??
                             _roleEvents.FirstOrDefault()?.TenantId ??
                             _permissionEvents.FirstOrDefault()?.TenantId ??
                             throw new InvalidOperationException("TenantId could not be determined from events.");
    
    public string TenantName => _tenantEvents.FirstOrDefault()?.TenantName ??
                                throw new InvalidOperationException("TenantName could not be determined from events.");
    
    private ICollection<PermissionEvent> _permissionEvents;
    private ICollection<RoleEvent> _roleEvents;
    private ICollection<UserSuspensionEvent> _suspensionEvents;
    private ICollection<UserTenantEvent> _tenantEvents;

    public UserAuthorization(
        ICollection<PermissionEvent> permissionEvents, 
        ICollection<RoleEvent> roleEvents, 
        ICollection<UserSuspensionEvent> suspensionEvents, 
        ICollection<UserTenantEvent> tenantEvents)
    {
        _permissionEvents = permissionEvents ?? throw new ArgumentNullException(nameof(permissionEvents));
        _roleEvents = roleEvents ?? throw new ArgumentNullException(nameof(roleEvents));
        _suspensionEvents = suspensionEvents ?? throw new ArgumentNullException(nameof(suspensionEvents));
        _tenantEvents = tenantEvents ?? throw new ArgumentNullException(nameof(tenantEvents));
        
        ValidateEventsConsistency();
        ValidateTenantEventActiveStatus();
    }

    

    private void ValidateEventsConsistency()
    {
        if (!_roleEvents.Any())
        {
            throw new InvalidOperationException(
                "At least one role event is required.");
        }

        if (!_tenantEvents.Any())
        {
            throw new InvalidOperationException(
                "At least one tenant event is required.");
        }

        var eventOrigins = _permissionEvents
            .Select(e => (e.UserId, e.TenantId))
            .Concat(_roleEvents.Select(e => (e.UserId, e.TenantId)))
            .Concat(_suspensionEvents.Select(e => (e.UserId, e.TenantId)))
            .Concat(_tenantEvents.Select(e => (e.UserId, e.TenantId)))
            .ToList();


        var (expectedUserId, expectedTenantId) = eventOrigins.First();

        var hasMismatch = eventOrigins.Any(origin =>
            origin.UserId != expectedUserId || origin.TenantId != expectedTenantId);

        if (hasMismatch)
        {
            throw new InvalidOperationException(
                "All authorization events must belong to the same UserId and TenantId.");
        }
    }
    
    private void ValidateTenantEventActiveStatus()
    {
        var activeTenantEvent = _tenantEvents
            .Where(e => e.Status == TenantMembershipStatus.Active)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault();

        if (activeTenantEvent == null)
        {
            throw new InvalidOperationException(
                "At least one active tenant event is required.");
        }
    }

    private void ValidateRoleEventActiveStatus()
    {
        var activeRoleEvent = _roleEvents
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault();

        if (activeRoleEvent == null)
        {
            throw new InvalidOperationException(
                "At least one role event is required.");
        }
    }
    
    public Role Role => _roleEvents
        .OrderByDescending(e => e.CreatedAt)
        .FirstOrDefault()?.Role ??
        throw new InvalidOperationException("Role could not be determined from events.");
    
    public void AddRole(Role role, DateTimeOffset createdAt, Guid createdBy)
    {
        if (IsRevoked(createdAt))
        {
            throw new InvalidOperationException("Cannot add role to a revoked user.");
        }

        var latestRole = _roleEvents
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => (Role?)e.Role)
            .FirstOrDefault();

        if (latestRole.HasValue && latestRole.Value == role)
        {
            return;
        }
        var newEvent = RoleEvent.CreateRoleAssignmentEvent(createdAt, createdBy, UserId, TenantId, role);
        _roleEvents.Add(newEvent);
    }
    
    public IEnumerable<Permission> Permissions
    {
        get
        {
            var grantedEvents = _permissionEvents
                .Where(e => e.Type == PermissionEventType.PermissionGrantedEvent)
                .ToList();

            var activePermissions = new List<Permission>();

            foreach (var grantedEvent in grantedEvents)
            {
                var hasLaterRevocation = _permissionEvents
                    .Any(pe =>
                        pe.Type == PermissionEventType.PermissionRevokedEvent &&
                        pe.Permission.Module == grantedEvent.Permission.Module &&
                        pe.Permission.Resource == grantedEvent.Permission.Resource &&
                        pe.Permission.Action == grantedEvent.Permission.Action &&
                        pe.CreatedAt > grantedEvent.CreatedAt);

                if (!hasLaterRevocation)
                {
                    activePermissions.Add(grantedEvent.Permission);
                }
            }

            return activePermissions.Distinct().ToList();
        }
    }

    public void AddPermission(
        Permission permission, 
        DateTimeOffset createdAt, 
        Guid createdBy)
    {
        if (IsRevoked(createdAt))
        {
            throw new InvalidOperationException("Cannot add permission to a revoked user.");
        }
        if (Permissions.Contains(permission))
        {
            return;
        }
        var newEvent = PermissionEvent.CreatePermissionGrantedEvent(
            createdAt,
            createdBy, 
            UserId, 
            TenantId, 
            permission);
        _permissionEvents.Add(newEvent);
    }

    public void RemovePermission(Permission permission, DateTimeOffset createdAt, Guid createdBy)
    {
        if (IsRevoked(createdAt))
        {
            throw new InvalidOperationException("Cannot add permission to a revoked user.");
        }
        
        if (!Permissions.Contains(permission))
            return;
        var newEvent = PermissionEvent.CreatePermissionRevokedEvent(
            createdAt,
            createdBy,
            UserId,
            TenantId,
            permission);
        _permissionEvents.Add(newEvent);
    }
    
    public IReadOnlyCollection<UserSuspensionDetails> GetActiveSuspensions(DateTimeOffset atTime)
    {
        var activeSuspensions = new List<UserSuspensionDetails>();
        
        var suspensionEvents = _suspensionEvents 
            .Where(e => 
                e.Type == SuspensionEventType.Created && 
                e.CreatedAt <= atTime &&
                (!e.EndedAt.HasValue || e.EndedAt.Value >= atTime));

        foreach (var suspensionEvent in suspensionEvents)
        {
            var canceledEvent = _suspensionEvents
                .FirstOrDefault(e => 
                    e.Type == SuspensionEventType.Cancelled && 
                    e.OriginalSuspensionEventId == suspensionEvent.Id);

            if (canceledEvent == null)
            {
                activeSuspensions.Add(new UserSuspensionDetails(
                    UserId,
                    TenantId,
                    suspensionEvent.Source!,
                    new Interval(suspensionEvent.StartedAt!.Value, suspensionEvent.EndedAt)));
            }
        }

        return activeSuspensions.AsReadOnly();
    }
    
    public bool IsRevoked(DateTimeOffset atTime)
    {
        return _tenantEvents
            .Where(e => e.CreatedAt <= atTime)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault()?.Status == TenantMembershipStatus.Revoked;
    }
    
    public bool IsSuspended(DateTimeOffset atTime)
    {
        var activeSuspensions = GetActiveSuspensions(atTime);
        return activeSuspensions.Any();
    }

    public bool IsActive(DateTimeOffset activatedAt)
    {
        return IsRevoked(activatedAt) == false && IsSuspended(activatedAt) == false;
    }

    public void Deactivate(Guid createdBy, SuspensionSource source, DateTimeOffset startedAt, DateTimeOffset? endedAt = null)
    {
        UserSuspensionEvent suspensionEvent;
        
        if (endedAt.HasValue)
        {
            suspensionEvent = UserSuspensionEvent.CreateTemporaryUserSuspensionEvent(
                createdBy,
                DateTimeOffset.UtcNow,
                UserId,
                TenantId,
                source,
                startedAt,
                endedAt.Value);
        }
        else
        {
            suspensionEvent = UserSuspensionEvent.CreateIndefiniteUserSuspensionEvent(
                createdBy,
                DateTimeOffset.UtcNow,
                UserId,
                TenantId,
                source);
        }
        
        _suspensionEvents.Add(suspensionEvent);
    }
    
    public void CancelSuspension(Guid originalSuspensionEventId, Guid createdBy, SuspensionSource source, DateTimeOffset cancelledAt)
    {
        var originalEvent = _suspensionEvents
            .FirstOrDefault(e => e.Id == originalSuspensionEventId);
        
        if (originalEvent == null || originalEvent.Type != SuspensionEventType.Created)
            return;

        var suspensionEvent = UserSuspensionEvent.CreateCancelledUserSuspensionEvent(
            originalSuspensionEventId,
            createdBy,
            cancelledAt,
            UserId,
            TenantId,
            source);
        
        _suspensionEvents.Add(suspensionEvent);
    }
    public HistoryUserAuthorizationContext GetHistoryContext()
    {
        var roleHistory = BuildRoleHistory();
        var permissionHistory = BuildPermissionHistory();
        var suspensionHistory = BuildSuspensionHistory();
        var tenantMembership = BuildTenantMembershipHistory();

        return new HistoryUserAuthorizationContext(
            UserId,
            TenantId,
            roleHistory,
            permissionHistory,
            suspensionHistory,
            tenantMembership);
    }

    internal IEnumerable<RoleEvent> GetRoleEvents() => _roleEvents;
    
    internal IEnumerable<PermissionEvent> GetPermissionEvents() => _permissionEvents;
    
    internal IEnumerable<UserSuspensionEvent> GetSuspensionEvents() => _suspensionEvents;

    private Dictionary<Role, IEnumerable<Interval>> BuildRoleHistory()
    {
        var roleHistory = new Dictionary<Role, IEnumerable<Interval>>();
        var sortedEvents = _roleEvents.OrderBy(e => e.CreatedAt).ToList();

        for (int i = 0; i < sortedEvents.Count; i++)
        {
            var currentEvent = sortedEvents[i];
            DateTimeOffset? endDate = i + 1 < sortedEvents.Count
                ? sortedEvents[i + 1].CreatedAt
                : null;

            var interval = new Interval(currentEvent.CreatedAt, endDate);

            if (!roleHistory.ContainsKey(currentEvent.Role))
            {
                roleHistory[currentEvent.Role] = new List<Interval>();
            }

            ((List<Interval>)roleHistory[currentEvent.Role]).Add(interval);
        }

        return roleHistory;
    }

    private Dictionary<Permission, IEnumerable<Interval>> BuildPermissionHistory()
    {
        var permissionHistory = new Dictionary<Permission, IEnumerable<Interval>>();
        var grantedEvents = _permissionEvents
            .Where(e => e.Type == PermissionEventType.PermissionGrantedEvent)
            .OrderBy(e => e.CreatedAt)
            .GroupBy(e => e.Permission);

        foreach (var permissionGroup in grantedEvents)
        {
            var intervals = new List<Interval>();
            var permissionEvents = permissionGroup.ToList();

            for (int i = 0; i < permissionEvents.Count; i++)
            {
                var grantEvent = permissionEvents[i];
                
                var revokeEvent = _permissionEvents
                    .Where(e => 
                        e.Type == PermissionEventType.PermissionRevokedEvent &&
                        e.Permission.Module == grantEvent.Permission.Module &&
                        e.Permission.Resource == grantEvent.Permission.Resource &&
                        e.Permission.Action == grantEvent.Permission.Action &&
                        e.CreatedAt > grantEvent.CreatedAt)
                    .OrderBy(e => e.CreatedAt)
                    .FirstOrDefault();

                DateTimeOffset? endDate = revokeEvent?.CreatedAt;
                intervals.Add(new Interval(grantEvent.CreatedAt, endDate));
            }

            permissionHistory[permissionGroup.Key] = intervals;
        }

        return permissionHistory;
    }

    private IEnumerable<UserSuspensionDetails> BuildSuspensionHistory()
    {
        var suspensionHistory = new List<UserSuspensionDetails>();
        var sortedEvents = _suspensionEvents
            .Where(e => e.Type == SuspensionEventType.Created)
            .OrderBy(e => e.CreatedAt)
            .ToList();

        foreach (var suspensionEvent in sortedEvents)
        {
            var cancelEvent = _suspensionEvents
                .FirstOrDefault(e =>
                    e.Type == SuspensionEventType.Cancelled &&
                    e.OriginalSuspensionEventId == suspensionEvent.Id);

            var startDate = suspensionEvent.StartedAt ?? suspensionEvent.CreatedAt;
            var endDate = cancelEvent?.CreatedAt ?? suspensionEvent.EndedAt;
            var interval = new Interval(startDate, endDate);

            suspensionHistory.Add(new UserSuspensionDetails(
                UserId,
                TenantId,
                suspensionEvent.Source!,
                interval));
        }

        return suspensionHistory;
    }

    private IEnumerable<Interval> BuildTenantMembershipHistory()
    {
        var sortedEvents = _tenantEvents
            .OrderBy(e => e.CreatedAt)
            .ToList();

        var history = new List<Interval>();

        for (var i = 0; i < sortedEvents.Count; i++)
        {
            var currentEvent = sortedEvents[i];
            var endDate = i + 1 < sortedEvents.Count
                ? sortedEvents[i + 1].CreatedAt
                : (DateTimeOffset?)null;

            history.Add(new Interval(currentEvent.CreatedAt, endDate));
        }

        return history;
    }
    
}
