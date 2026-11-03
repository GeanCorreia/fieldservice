using FieldService.AccountManagement.Events;
using FieldService.AccountManagement.Interfaces;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Entities;

internal class PersonalInfo : ITenantEntity
{
    protected PersonalInfo()
    {
        Events = new List<TenantEventBase>();
    }

    public PersonalInfo(ICollection<TenantEventBase> events)
    {
        Events = events ?? throw new ArgumentNullException(nameof(events));
        ValidateEventsConsistency();
    }

    public ICollection<TenantEventBase> Events { get; private set; }

    public Guid UserId => GetRegisteredEvent().UserId;

    public Guid TenantId => GetRegisteredEvent().TenantId;

    public string FullName => Events
        .OfType<PersonalInfoFullNameUpdatedEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.FullName)
        .FirstOrDefault()
        ?? GetRegisteredEvent().FullName
        ?? throw new InvalidOperationException("FullName could not be determined from events.");

    public string DisplayName => Events
        .OfType<PersonalInfoDisplayNameUpdatedEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.DisplayName)
        .FirstOrDefault()
        ?? GetRegisteredEvent().DisplayName
        ?? throw new InvalidOperationException("DisplayName could not be determined from events.");

    public Email Email => Events
        .OfType<PersonalInfoEmailUpdatedEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.Email)
        .FirstOrDefault()
        ?? GetRegisteredEvent().Email
        ?? throw new InvalidOperationException("Email could not be determined from events.");

    public Phone Phone => Events
        .OfType<PersonalInfoPhoneUpdatedEvent>()
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => e.Phone)
        .FirstOrDefault()
        ?? GetRegisteredEvent().Phone
        ?? throw new InvalidOperationException("Phone could not be determined from events.");

    public static PersonalInfo Create(
        Guid userId,
        Guid tenantId,
        string fullName,
        string displayName,
        Email email,
        Phone phone,
        Guid createdBy,
        DateTimeOffset? createdAt = null)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;

        var events = new List<TenantEventBase>
        {
            PersonalInfoRegisteredEvent.Create(userId, tenantId, fullName, displayName, email, phone, createdBy, effectiveCreatedAt)
        };

        return new PersonalInfo(events);
    }

    public void UpdateFullName(string fullName, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("FullName is required.", nameof(fullName));

        var normalizedFullName = fullName.Trim();

        if (string.Equals(FullName, normalizedFullName, StringComparison.Ordinal))
            return;

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        Events.Add(PersonalInfoFullNameUpdatedEvent.Create(
            UserId,
            TenantId,
            normalizedFullName,
            createdBy,
            effectiveCreatedAt));
    }

    public void UpdateDisplayName(string displayName, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("DisplayName is required.", nameof(displayName));

        var normalizedDisplayName = displayName.Trim();

        if (string.Equals(DisplayName, normalizedDisplayName, StringComparison.Ordinal))
            return;

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        Events.Add(PersonalInfoDisplayNameUpdatedEvent.Create(
            UserId,
            TenantId,
            normalizedDisplayName,
            createdBy,
            effectiveCreatedAt));
    }

    public void UpdateEmail(Email email, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (Email == email)
            return;

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        Events.Add(PersonalInfoEmailUpdatedEvent.Create(
            UserId,
            TenantId,
            email,
            createdBy,
            effectiveCreatedAt));
    }

    public void UpdatePhone(Phone phone, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (Phone == phone)
            return;

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        Events.Add(PersonalInfoPhoneUpdatedEvent.Create(
            UserId,
            TenantId,
            phone,
            createdBy,
            effectiveCreatedAt));
    }

    internal IEnumerable<PersonalInfoFullNameUpdatedEvent> GetFullNameEvents() => Events.OfType<PersonalInfoFullNameUpdatedEvent>();

    internal IEnumerable<PersonalInfoRegisteredEvent> GetRegisteredEvents() => Events.OfType<PersonalInfoRegisteredEvent>();

    internal IEnumerable<PersonalInfoDisplayNameUpdatedEvent> GetDisplayNameEvents() => Events.OfType<PersonalInfoDisplayNameUpdatedEvent>();

    internal IEnumerable<PersonalInfoEmailUpdatedEvent> GetEmailEvents() => Events.OfType<PersonalInfoEmailUpdatedEvent>();

    internal IEnumerable<PersonalInfoPhoneUpdatedEvent> GetPhoneEvents() => Events.OfType<PersonalInfoPhoneUpdatedEvent>();

    private void ValidateEventsConsistency()
    {
        var registeredEvent = GetRegisteredEvent();

        var registeredEvents = Events.OfType<PersonalInfoRegisteredEvent>().ToList();
        if (registeredEvents.Count == 0)
            throw new InvalidOperationException("At least one registered event is required.");

        var fullNameEvents = Events.OfType<PersonalInfoFullNameUpdatedEvent>().ToList();

        var displayNameEvents = Events.OfType<PersonalInfoDisplayNameUpdatedEvent>().ToList();

        var emailEvents = Events.OfType<PersonalInfoEmailUpdatedEvent>().ToList();

        var phoneEvents = Events.OfType<PersonalInfoPhoneUpdatedEvent>().ToList();

        var origins = registeredEvents
            .Select(e => (e.UserId, e.TenantId))
            .Concat(fullNameEvents.Select(e => (e.UserId, e.TenantId)))
            .Concat(displayNameEvents.Select(e => (e.UserId, e.TenantId)))
            .Concat(emailEvents.Select(e => (e.UserId, e.TenantId)))
            .Concat(phoneEvents.Select(e => (e.UserId, e.TenantId)))
            .ToList();

        var hasMismatch = origins.Any(origin =>
            origin.UserId != registeredEvent.UserId ||
            origin.TenantId != registeredEvent.TenantId);

        if (hasMismatch)
            throw new InvalidOperationException("All personal info events must belong to the same User and Tenant.");
    }

    private PersonalInfoRegisteredEvent GetRegisteredEvent()
    {
        return Events
            .OfType<PersonalInfoRegisteredEvent>()
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Personal info registered event is required.");
    }
}

