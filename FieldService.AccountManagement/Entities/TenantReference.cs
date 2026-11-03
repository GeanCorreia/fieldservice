using FieldService.AccountManagement.Events;
using FieldService.AccountManagement.Interfaces;

namespace FieldService.AccountManagement.Entities;

internal class TenantReference : ITenantEntity
{
    public TenantReference(ICollection<TenantEventBase> events)
    {
        Events = events ?? throw new ArgumentNullException(nameof(events));

        var createdEvent = Events
            .OfType<TenantCreatedEvent>()
            .OrderBy(e => e.CreatedAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("At least one tenant created event is required.");

        TenantId = createdEvent.TenantId;

        var invalidEvent = Events.FirstOrDefault(e =>
            e is not (TenantCreatedEvent or TenantNameUpdatedEvent) || e.TenantId != TenantId);

        if (invalidEvent is not null)
            throw new InvalidOperationException(
                "Tenant reference events must be tenant created/name updated events of the same TenantId.");
    }

    public Guid TenantId { get; }

    public ICollection<TenantEventBase> Events { get; }

    public string Name => Events
        .OfType<TenantNameUpdatedEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.Name)
        .FirstOrDefault()
        ?? Events
            .OfType<TenantCreatedEvent>()
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.Name)
            .FirstOrDefault()
        ?? throw new InvalidOperationException("Tenant name could not be determined from events.");
    
    public void UpdateName(string name, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.Equals(Name, name?.Trim(), StringComparison.Ordinal))
            return;

        Events.Add(TenantNameUpdatedEvent.Create(
            TenantId,
            name,
            createdBy,
            createdAt));
    }
}

