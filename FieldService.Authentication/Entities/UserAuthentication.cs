using FieldService.Authentication.Types;

namespace FieldService.Authentication.Entities;

public class UserAuthentication
{
    public Guid UserId { get; private set; }
    public string ExternalId { get; private set; }
    
    
    public AuthenticationProvider Provider { get; private set; }

    protected UserAuthentication() { }

    public UserAuthentication(
        Guid userId, 
        string externalId, 
        AuthenticationProvider provider)
    {
        if (userId == default)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("ExternalId is required.", nameof(externalId));
        if (!Enum.IsDefined(typeof(AuthenticationProvider), provider))
            throw new ArgumentException("Provider is required.", nameof(provider));

        UserId = userId;
        ExternalId = externalId;
        Provider = provider;
        
    }
}