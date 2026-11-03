using FieldService.AccountManagement.Events;
using FieldService.AccountManagement.Interfaces;

namespace FieldService.AccountManagement.Entities;

internal class JobTitle : ITenantEntity
{
    public static string DefaultTitle => "Geral";
    
    protected JobTitle()
    {
        Events = new List<TenantEventBase>();
    }

    public JobTitle(ICollection<TenantEventBase> events)
    {
        Events = events ?? throw new ArgumentNullException(nameof(events));
        ValidateEventsConsistency();
    }

    public ICollection<TenantEventBase> Events { get; private set; }

    public Guid Id => Events
        .OfType<JobTitleRegisteredEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.JobTitleId)
        .FirstOrDefault();

    public Guid TenantId => Events
        .OfType<JobTitleRegisteredEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.TenantId)
        .FirstOrDefault();

    public string Title => Events
        .OfType<JobTitleTitleUpdatedEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.Title)
        .FirstOrDefault()
        ?? Events
        .OfType<JobTitleRegisteredEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.Title)
        .FirstOrDefault()
        ?? throw new InvalidOperationException("Title could not be determined from events.");

    public string? Description => Events
        .OfType<JobTitleDescriptionUpdatedEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.Description)
        .FirstOrDefault();

    public bool IsDeactivated => Events.OfType<JobTitleDeactivatedEvent>().Any();

    public static JobTitle Create(
        Guid id,
        Guid tenantId,
        string title,
        string? description,
        Guid createdBy,
        DateTimeOffset? createdAt = null)
    {
        if (id == default)
            throw new ArgumentException("Id is required.", nameof(id));

        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;

        var events = new List<TenantEventBase>
        {
            JobTitleRegisteredEvent.Create(id, tenantId, title, createdBy, effectiveCreatedAt)
        };

        if (description is not null)
        {
            events.Add(JobTitleDescriptionUpdatedEvent.Create(
                id,
                tenantId,
                description,
                createdBy,
                effectiveCreatedAt));
        }

        return new JobTitle(events);
    }

    public void UpdateTitle(string title, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        var normalizedTitle = title.Trim();
        if (string.Equals(Title, normalizedTitle, StringComparison.Ordinal))
            return;

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        Events.Add(JobTitleTitleUpdatedEvent.Create(
            Id,
            TenantId,
            normalizedTitle,
            createdBy,
            effectiveCreatedAt));
    }

    public void UpdateDescription(string? description, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (description is null)
            return;

        var normalizedDescription = string.IsNullOrEmpty(description) ? null : description;
        if (string.Equals(Description, normalizedDescription, StringComparison.Ordinal))
            return;

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        Events.Add(JobTitleDescriptionUpdatedEvent.Create(
            Id,
            TenantId,
            normalizedDescription,
            createdBy,
            effectiveCreatedAt));
    }

    public void Deactivate(Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (IsDeactivated)
            return;

        Events.Add(JobTitleDeactivatedEvent.Create(Id, TenantId, createdBy, createdAt));
    }

    internal IEnumerable<JobTitleRegisteredEvent> GetTitleEvents() => Events.OfType<JobTitleRegisteredEvent>();

    internal IEnumerable<JobTitleTitleUpdatedEvent> GetTitleUpdatedEvents() => Events.OfType<JobTitleTitleUpdatedEvent>();

    internal IEnumerable<JobTitleDescriptionUpdatedEvent> GetDescriptionEvents() => Events.OfType<JobTitleDescriptionUpdatedEvent>();

    private void ValidateEventsConsistency()
    {
        var titleEvents = Events.OfType<JobTitleRegisteredEvent>().ToList();
        if (titleEvents.Count == 0)
            throw new InvalidOperationException("At least one title event is required.");

        var titleUpdatedEvents = Events.OfType<JobTitleTitleUpdatedEvent>().ToList();
        var descriptionEvents = Events.OfType<JobTitleDescriptionUpdatedEvent>().ToList();
        var deactivatedEvents = Events.OfType<JobTitleDeactivatedEvent>().ToList();

        var origins = titleEvents
            .Select(e => (e.JobTitleId, e.TenantId))
            .Concat(titleUpdatedEvents.Select(e => (e.JobTitleId, e.TenantId)))
            .Concat(descriptionEvents.Select(e => (e.JobTitleId, e.TenantId)))
            .Concat(deactivatedEvents.Select(e => (e.JobTitleId, e.TenantId)))
            .ToList();

        var expected = origins.First();
        var hasMismatch = origins.Any(origin =>
            origin.JobTitleId != expected.JobTitleId ||
            origin.TenantId != expected.TenantId);

        if (hasMismatch)
            throw new InvalidOperationException("All events must belong to the same JobTitle and Tenant.");
    }
}