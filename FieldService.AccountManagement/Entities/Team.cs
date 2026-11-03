using FieldService.AccountManagement.Events;
using FieldService.AccountManagement.Interfaces;

namespace FieldService.AccountManagement.Entities;

internal class Team : ITenantEntity
{
    public static string DefaultTeamName => "Geral";
    
    // Required by EF materialization.
    protected Team()
    {
        Events = new List<TenantEventBase>();
    }

    public Team(ICollection<TenantEventBase> events)
    {
        Events = events ?? throw new ArgumentNullException(nameof(events));
        ValidateEventsConsistency();
    }

    public ICollection<TenantEventBase> Events { get; private set; }

    public Guid Id => GetRegisteredEvent().TeamId;

    public Guid TenantId => GetRegisteredEvent().TenantId;

    public string Name => Events
        .OfType<TeamNameUpdatedEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.Name)
        .FirstOrDefault()
        ?? GetRegisteredEvent().Name
        ?? throw new InvalidOperationException("Name could not be determined from events.");

    public string? Description
    {
        get
        {
            var latestUpdate = Events
                .OfType<TeamDescriptionUpdatedEvent>()
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            return latestUpdate is not null ? latestUpdate.Description : GetRegisteredEvent().Description;
        }
    }

    public Guid? LeaderId => Events
        .OfType<TeamLeaderUpdatedEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => new { e.LeaderId })
        .FirstOrDefault()
        is { } updated ? updated.LeaderId : GetRegisteredEvent().LeaderId;

    public bool IsDeactivated => Events
        .OfType<TeamDeactivatedEvent>()
        .Any();

    
    public string? LeaderName => GetLeaderName();


    private string? GetLeaderName()
    {
        if (LeaderId == null)
        {
            return null;
        }
        
        return Events
            .OfType<PersonalInfoDisplayNameUpdatedEvent>()
            .Where(e => e.UserId == LeaderId && e.TenantId == TenantId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.DisplayName)
            .FirstOrDefault()
            ?? Events
                .OfType<PersonalInfoRegisteredEvent>()
                .Where(e => e.UserId == LeaderId && e.TenantId == TenantId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => e.DisplayName)
                .FirstOrDefault();
    }
    
    public static TeamRegisteredEvent CreateDefaultTeam(Guid tenantId, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        return TeamRegisteredEvent.Create(
            Guid.NewGuid(),
            tenantId, 
            DefaultTeamName, 
            null, 
            null, 
            createdBy, 
            createdAt);
    }
    

    public static Team Create(
        Guid id,
        Guid tenantId,
        string name,
        string? description,
        Guid? leaderId,
        Guid createdBy,
        DateTimeOffset? createdAt = null)
    {
        if (id == default)
            throw new ArgumentException("Id is required.", nameof(id));

        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;

        var registeredEvent = TeamRegisteredEvent.Create(id, tenantId, name, description, leaderId, createdBy, effectiveCreatedAt);
        var team = new Team();
        team.Events.Add(registeredEvent);
        return team;
    }

    public void UpdateDescription(string? description, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (description is null)
            return;

        var normalizedDescription = string.IsNullOrEmpty(description) ? null : description;
        if (string.Equals(Description, normalizedDescription, StringComparison.Ordinal))
            return;

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        Events.Add(TeamDescriptionUpdatedEvent.Create(
            Id,
            TenantId,
            normalizedDescription,
            createdBy,
            effectiveCreatedAt));
    }
    
    public void UpdateName(string name, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        var normalizedName = name.Trim();
        if (string.Equals(Name, normalizedName, StringComparison.Ordinal))
            return;

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        Events.Add(TeamNameUpdatedEvent.Create(
            Id,
            TenantId,
            normalizedName,
            createdBy,
            effectiveCreatedAt));
    }

    public void UpdateLeader(Guid? leaderId, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (LeaderId == leaderId)
            return;

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        Events.Add(TeamLeaderUpdatedEvent.Create(
            Id,
            TenantId,
            leaderId,
            createdBy,
            effectiveCreatedAt));
    }

    public void RemoveLeader(Guid createdBy, DateTimeOffset? createdAt = null)
    {
        UpdateLeader(null, createdBy, createdAt);
    }

    public void Deactivate(Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (IsDeactivated)
            return;

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        Events.Add(TeamDeactivatedEvent.Create(
            Id,
            TenantId,
            createdBy,
            effectiveCreatedAt));
    }
    
    internal IEnumerable<TeamNameUpdatedEvent> GetNameEvents() => Events.OfType<TeamNameUpdatedEvent>();

    internal IEnumerable<TeamRegisteredEvent> GetRegisteredEvents() => Events.OfType<TeamRegisteredEvent>();

    internal IEnumerable<TeamDescriptionUpdatedEvent> GetDescriptionEvents() => Events.OfType<TeamDescriptionUpdatedEvent>();

    internal IEnumerable<TeamLeaderUpdatedEvent> GetLeaderEvents() => Events.OfType<TeamLeaderUpdatedEvent>();

    internal IEnumerable<TeamDeactivatedEvent> GetDeactivatedEvents() => Events.OfType<TeamDeactivatedEvent>();

    internal static List<Team> BuildTeams(IReadOnlyCollection<TenantEventBase> events)
    {
        var leaderEvents = events
            .Where(e => e is PersonalInfoRegisteredEvent or PersonalInfoDisplayNameUpdatedEvent)
            .ToLookup(e => e is PersonalInfoRegisteredEvent registered
                ? registered.UserId
                : ((PersonalInfoDisplayNameUpdatedEvent)e).UserId);

        return events
            .Where(e => e is TeamRegisteredEvent or TeamNameUpdatedEvent or TeamDescriptionUpdatedEvent
                or TeamLeaderUpdatedEvent or TeamDeactivatedEvent)
            .GroupBy(e => e switch
            {
                TeamRegisteredEvent x => x.TeamId,
                TeamNameUpdatedEvent x => x.TeamId,
                TeamDescriptionUpdatedEvent x => x.TeamId,
                TeamLeaderUpdatedEvent x => x.TeamId,
                TeamDeactivatedEvent x => x.TeamId,
                _ => throw new InvalidOperationException($"Event {e.GetType().Name} is not a team event.")
            })
            .Where(group => group.OfType<TeamRegisteredEvent>().Any())
            .Select(group =>
            {
                var team = new Team(group.OrderBy(e => e.CreatedAt).ToList());
                if (team.LeaderId is { } leaderId)
                {
                    foreach (var leaderEvent in leaderEvents[leaderId].Where(e => e.TenantId == team.TenantId))
                        team.Events.Add(leaderEvent);
                }

                return team;
            })
            .ToList();
    }

    private void ValidateEventsConsistency()
    {
        var registeredEvent = GetRegisteredEvent();

        var registeredEvents = Events.OfType<TeamRegisteredEvent>().ToList();
        if (registeredEvents.Count == 0)
            throw new InvalidOperationException("At least one registered event is required.");

        var nameEvents = Events.OfType<TeamNameUpdatedEvent>().ToList();
        var descriptionEvents = Events.OfType<TeamDescriptionUpdatedEvent>().ToList();
        var leaderEvents = Events.OfType<TeamLeaderUpdatedEvent>().ToList();
        var deactivatedEvents = Events.OfType<TeamDeactivatedEvent>().ToList();

        var origins = registeredEvents
            .Select(e => (e.TeamId, e.TenantId))
            .Concat(nameEvents.Select(e => (e.TeamId, e.TenantId)))
            .Concat(descriptionEvents.Select(e => (e.TeamId, e.TenantId)))
            .Concat(leaderEvents.Select(e => (e.TeamId, e.TenantId)))
            .Concat(deactivatedEvents.Select(e => (e.TeamId, e.TenantId)))
            .ToList();

        var hasMismatch = origins.Any(origin =>
            origin.TeamId != registeredEvent.TeamId ||
            origin.TenantId != registeredEvent.TenantId);

        if (hasMismatch)
            throw new InvalidOperationException("All events must belong to the same Team and Tenant.");
    }

    private TeamRegisteredEvent GetRegisteredEvent()
    {
        return Events
            .OfType<TeamRegisteredEvent>()
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Team registered event is required.");
    }
}