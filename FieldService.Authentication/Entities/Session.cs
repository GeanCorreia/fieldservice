using FieldService.Authentication.Types;

namespace FieldService.Authentication.Entities;

public enum RevocationReason
{
    Logout = 1,
    Inactivity =2,
    UserDisabled = 3,
    Security = 4,
    Timeout = 5
}

public class Session
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string ExternalId { get; private set; }
    public AuthenticationProvider Provider { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public RevocationReason? RevocationReason { get; private set; }
    
    protected Session()
    {
       
    }

    public Session(
        Guid id, 
        Guid userId,
        Guid tenantId,
        string externalId,
        AuthenticationProvider provider,
        DateTimeOffset startedAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? revokedAt = null, 
        RevocationReason? revocationReason = null
        )
    {
        Id = id;
        UserId = userId;
        TenantId = tenantId;
        ExternalId = externalId;
        Provider = provider;
        StartedAt = startedAt;
        RevokedAt = revokedAt;
        RevocationReason = revocationReason;
        ExpiresAt = expiresAt;
    }


   
    public void UpdateExpiration(DateTimeOffset newExpiration)
    {
        if (newExpiration <= StartedAt)
        {
            throw new ArgumentException("New expiration must be after the session start time.", nameof(newExpiration));
        }
        
        if(newExpiration <= ExpiresAt)
        {
            throw new ArgumentException("New expiration must be after the current expiration time.", nameof(newExpiration));
        }
        ExpiresAt = newExpiration;
    }

    public void Revoke(DateTimeOffset utcNow, RevocationReason reason)
    {
        if (reason == Entities.RevocationReason.Timeout)
        {
            throw new ArgumentException("Inactivity/Timeout revocation should be " +
                                        "handled by the IsActive method.", nameof(reason));
        }
        RevokedAt = utcNow;
        RevocationReason = reason;
    }
    public bool IsRevoked => RevokedAt.HasValue;
    
    public bool IsActive()
    {
        if (RevokedAt.HasValue || RevocationReason.HasValue)
        {
            return false;
        }
        
        return true;
    }

    public bool ShouldExpires(DateTimeOffset now)
    {
        if (!IsActive())
        {
            return false;
        }
        return ExpiresAt <= now;
    }

}