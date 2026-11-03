namespace FieldService.AccountManagement.Events;

public class PersonalInfoDisplayNameUpdatedEvent : TenantEventBase
{
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; }

    protected PersonalInfoDisplayNameUpdatedEvent()
    {
        DisplayName = null!;
    }

    private PersonalInfoDisplayNameUpdatedEvent(
        Guid userId,
        Guid tenantId,
        string displayName,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.PersonalInfoDisplayNameUpdated, id)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("DisplayName is required.", nameof(displayName));

        UserId = userId;
        DisplayName = displayName.Trim();
    }

    public static PersonalInfoDisplayNameUpdatedEvent Create(
        Guid userId,
        Guid tenantId,
        string displayName,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new PersonalInfoDisplayNameUpdatedEvent(
            userId,
            tenantId,
            displayName,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

