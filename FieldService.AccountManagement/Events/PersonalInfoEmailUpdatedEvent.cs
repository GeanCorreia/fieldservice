using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Events;

public class PersonalInfoEmailUpdatedEvent : TenantEventBase
{
    public Guid UserId { get; private set; }
    public Email Email { get; private set; }

    protected PersonalInfoEmailUpdatedEvent()
    {
        Email = null!;
    }

    private PersonalInfoEmailUpdatedEvent(
        Guid userId,
        Guid tenantId,
        Email email,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.PersonalInfoEmailUpdated, id)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        Email = email ?? throw new ArgumentNullException(nameof(email));
        UserId = userId;
    }

    public static PersonalInfoEmailUpdatedEvent Create(
        Guid userId,
        Guid tenantId,
        Email email,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new PersonalInfoEmailUpdatedEvent(
            userId,
            tenantId,
            email,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

