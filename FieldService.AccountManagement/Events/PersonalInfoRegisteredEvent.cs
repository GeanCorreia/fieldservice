using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Events;

public class PersonalInfoRegisteredEvent : TenantEventBase
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; }
    public string DisplayName { get; private set; }
    public Email Email { get; private set; }
    public Phone Phone { get; private set; }

    protected PersonalInfoRegisteredEvent()
    {
        FullName = null!;
        DisplayName = null!;
        Email = null!;
        Phone = null!;
    }

    private PersonalInfoRegisteredEvent(
        Guid userId,
        Guid tenantId,
        string fullName,
        string displayName,
        Email email,
        Phone phone,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? id = null)
        : base(tenantId, createdBy, createdAt, TenantEventType.PersonalInfoRegistered, id)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("FullName is required.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("DisplayName is required.", nameof(displayName));

        UserId = userId;
        FullName = fullName.Trim();
        DisplayName = displayName.Trim();
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Phone = phone ?? throw new ArgumentNullException(nameof(phone));
    }

    public static PersonalInfoRegisteredEvent Create(
        Guid userId,
        Guid tenantId,
        string fullName,
        string displayName,
        Email email,
        Phone phone,
        Guid createdBy,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        return new PersonalInfoRegisteredEvent(
            userId,
            tenantId,
            fullName,
            displayName,
            email,
            phone,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow,
            id);
    }
}

