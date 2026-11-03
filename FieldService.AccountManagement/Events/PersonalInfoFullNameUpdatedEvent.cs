namespace FieldService.AccountManagement.Events;

public class PersonalInfoFullNameUpdatedEvent : TenantEventBase
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; }

    protected PersonalInfoFullNameUpdatedEvent()
    {
        FullName = null!;
    }

    private PersonalInfoFullNameUpdatedEvent(
        Guid userId,
        Guid tenantId,
        string fullName,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.PersonalInfoFullNameUpdated, id)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("FullName is required.", nameof(fullName));

        UserId = userId;
        FullName = fullName.Trim();
    }

    public static PersonalInfoFullNameUpdatedEvent Create(
        Guid userId,
        Guid tenantId,
        string fullName,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new PersonalInfoFullNameUpdatedEvent(
            userId,
            tenantId,
            fullName,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

