using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FieldService.AccountManagement.Events;
using FieldService.AccountManagement.Interfaces;

namespace FieldService.AccountManagement.Entities;

internal class Tenant : ITenantEntity
{
    // Required by EF materialization.
    protected Tenant()
    {
        Events = new List<TenantEventBase>();
    }

    public Tenant(ICollection<TenantEventBase> events)
    {
        Events = events ?? throw new ArgumentNullException(nameof(events));

        var createdEvent = Events
            .OfType<TenantCreatedEvent>()
            .OrderBy(e => e.CreatedAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("At least one tenant created event is required.");

        TenantId = createdEvent.TenantId;

        var invalidTenantEvent = Events.FirstOrDefault(e => e.TenantId != TenantId);
        if (invalidTenantEvent is not null)
            throw new InvalidOperationException("All tenant events must belong to the same TenantId.");
    }

    [Key]
    public Guid TenantId { get; private set; }

    public ICollection<TenantEventBase> Events { get; private set; }

    [NotMapped]
    public TenantReference Reference => BuildReference();

    [NotMapped]
    public IReadOnlyCollection<Team> Teams => BuildTeams();

    [NotMapped]
    public IReadOnlyCollection<JobTitle> JobTitles => BuildJobTitles();

    public static Tenant Create(Guid tenantId, string name, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;

        return new Tenant(new List<TenantEventBase>
        {
            TenantCreatedEvent.Create(tenantId, name, createdBy, effectiveCreatedAt),
            Team.CreateDefaultTeam(tenantId, createdBy, effectiveCreatedAt)
        });
    }
    
    public void UpdateName(string name, Guid updatedBy, DateTimeOffset? updatedAt = null)
    {
        var effectiveUpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be null or whitespace.", nameof(name));

        var lastNameEvent = Events
            .OfType<TenantNameUpdatedEvent>()
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault();

        if (lastNameEvent is not null && lastNameEvent.Name == name)
            return;

        Events.Add(TenantNameUpdatedEvent.Create(TenantId, name, updatedBy, effectiveUpdatedAt));
    }
    
    private TenantReference BuildReference()
    {
        var tenantCreatedEvents = Events
            .OfType<TenantCreatedEvent>()
            .Where(e => e.TenantId == TenantId)
            .ToList();

        var tenantNameEvents = Events
            .OfType<TenantNameUpdatedEvent>()
            .Where(e => e.TenantId == TenantId)
            .ToList();

        return new TenantReference(
            tenantCreatedEvents.Cast<TenantEventBase>()
                .Concat(tenantNameEvents)
                .OrderBy(e => e.CreatedAt)
                .ToList());
    }

    private IReadOnlyCollection<Team> BuildTeams()
    {
        return Team.BuildTeams(Events.Where(e => e.TenantId == TenantId).ToList());
    }

    private IReadOnlyCollection<JobTitle> BuildJobTitles()
    {
        var jobTitleEvents = Events
            .OfType<JobTitleRegisteredEvent>()
            .Where(e => e.TenantId == TenantId)
            .ToList();

        var jobTitleDescriptionEvents = Events
            .OfType<JobTitleDescriptionUpdatedEvent>()
            .Where(e => e.TenantId == TenantId)
            .ToList();

        var jobTitleDeactivatedEvents = Events
            .OfType<JobTitleDeactivatedEvent>()
            .Where(e => e.TenantId == TenantId)
            .ToList();

        var jobTitleTitleUpdatedEvents = Events
            .OfType<JobTitleTitleUpdatedEvent>()
            .Where(e => e.TenantId == TenantId)
            .ToList();

        var jobTitleIds = jobTitleEvents
            .Select(e => e.JobTitleId)
            .Distinct()
            .ToList();

        return jobTitleIds
            .Select(jobTitleId => new JobTitle(
                jobTitleEvents.Where(e => e.JobTitleId == jobTitleId).Cast<TenantEventBase>()
                    .Concat(jobTitleTitleUpdatedEvents.Where(e => e.JobTitleId == jobTitleId))
                    .Concat(jobTitleDescriptionEvents.Where(e => e.JobTitleId == jobTitleId))
                    .Concat(jobTitleDeactivatedEvents.Where(e => e.JobTitleId == jobTitleId))
                    .OrderBy(e => e.CreatedAt)
                    .ToList()))
            .ToList();
    }

    internal IEnumerable<TenantCreatedEvent> GetCreatedEvents() => Events.OfType<TenantCreatedEvent>();
    internal IEnumerable<TenantNameUpdatedEvent> GetNameEvents() => Events.OfType<TenantNameUpdatedEvent>();
}