using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Events;

public class PersonalInfoPhoneUpdatedEvent : TenantEventBase
{
    public Guid UserId { get; private set; }
    public Phone Phone { get; private set; }

    protected PersonalInfoPhoneUpdatedEvent()
    {
        Phone = null!;
    }

    private PersonalInfoPhoneUpdatedEvent(
        Guid userId,
        Guid tenantId,
        Phone phone,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.PersonalInfoPhoneUpdated, id)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        Phone = phone ?? throw new ArgumentNullException(nameof(phone));
        UserId = userId;
    }

    public static PersonalInfoPhoneUpdatedEvent Create(
        Guid userId,
        Guid tenantId,
        Phone phone,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new PersonalInfoPhoneUpdatedEvent(
            userId,
            tenantId,
            phone,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

