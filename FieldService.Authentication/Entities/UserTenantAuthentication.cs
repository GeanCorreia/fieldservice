namespace FieldService.Authentication.Entities;

public class UserTenantAuthentication
{

    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }

    protected UserTenantAuthentication() { }

    public UserTenantAuthentication(Guid userId, Guid tenantId, string name)
    {

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (userId == default)
        {
            throw new ArgumentException("User is required.", nameof(userId));
        }
        
        UserId = userId;
        TenantId = tenantId;
        Name = name;
    }
}