using FieldService.Authorization.Events;
using FieldService.Authorization.Types;
using FieldService.Shared.Types;

namespace FieldService.Authorization.Entities;

public class UserAuthorizationContext
{
    public Guid Id { get;}
    public Guid UserId { get; }
    public Guid TenantId { get; }
    
    private ICollection<PermissionEvent> _permissionEvents;
    private ICollection<RoleEvent> _roleEvents;
    private ICollection<UserSuspensionEvent> _suspensionEvents;

    public UserAuthorizationContext(
        ICollection<PermissionEvent> permissionEvents, 
        ICollection<RoleEvent> roleEvents, 
        ICollection<UserSuspensionEvent> suspensionEvents, 
        Guid id, 
        Guid userId, 
        Guid tenantId)
    {
        _permissionEvents = permissionEvents;
        _roleEvents = roleEvents;
        _suspensionEvents = suspensionEvents;
        Id = id;
        UserId = userId;
        TenantId = tenantId;
    }

    public Role Role => _roleEvents
        .OrderByDescending(e => e.CreatedAt)
        .FirstOrDefault()!.Role; 
        
    public void AddRole(Role role, DateTimeOffset createdAt, Guid createdBy)
    {
        if (Role == role)
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

    public bool IsActive(DateTimeOffset activatedAt)
    {
        var activeSuspensions = GetActiveSuspensions(activatedAt);
        return !activeSuspensions.Any();
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

        return new HistoryUserAuthorizationContext(
            UserId,
            TenantId,
            roleHistory,
            permissionHistory,
            suspensionHistory);
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
    
}
