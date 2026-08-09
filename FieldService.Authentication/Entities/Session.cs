using FieldService.Authentication.Types;
using FieldService.Shared.Services;

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
    public DateTime StartedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    private ICollection<SessionActivity> _activities;
    public DateTime? RevokedAt { get; private set; }
    public RevocationReason? RevocationReason { get; private set; }
    public DateTime? LastActivityAt { get; private set; } 
    public IReadOnlyCollection<SessionActivity> Activities => _activities
        .ToList().AsReadOnly();
    
    protected Session()
    {
       
    }

    public Session(
        IEnumerable<SessionActivity> activities, 
        Guid id, 
        Guid userId,
        Guid tenantId,
        string externalId,
        AuthenticationProvider provider,
        DateTime startedAt,
        DateTime expiresAt,
        DateTime? revokedAt = null, 
        RevocationReason? revocationReason = null, 
        DateTime? lastActivityAt = null
        )
    {
        _activities = activities.ToHashSet();
        Id = id;
        UserId = userId;
        TenantId = tenantId;
        ExternalId = externalId;
        Provider = provider;
        StartedAt = DateTimeService.EnsureUtc(startedAt);
        RevokedAt = DateTimeService.EnsureUtc(revokedAt);
        RevocationReason = revocationReason;
        LastActivityAt = DateTimeService.EnsureUtc(lastActivityAt);
        ExpiresAt = DateTimeService.EnsureUtc(expiresAt);
    }

    public void Touch(SessionActivity  activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        LastActivityAt = DateTimeService.EnsureUtc(activity.Timestamp);
        _activities.Add(activity);
    }

    internal IReadOnlyCollection<SessionActivity> GetActivities()
    {
        return Activities;
    }
    
    public void UpdateExpiration(DateTime newExpiration)
    {
        newExpiration = DateTimeService.EnsureUtc(newExpiration);
        if (newExpiration <= StartedAt)
        {
            throw new ArgumentException("New expiration must be after the session start time.", nameof(newExpiration));
        }
        ExpiresAt = newExpiration;
    }

    public void Revoke(DateTime utcNow, RevocationReason reason)
    {
        utcNow = DateTimeService.EnsureUtc(utcNow);
        if (reason == Entities.RevocationReason.Timeout)
        {
            throw new ArgumentException("Inactivity/Timeout revocation should be " +
                                        "handled by the IsActive method.", nameof(reason));
        }
        RevokedAt = utcNow;
        RevocationReason = reason;
    }
    public bool IsRevoked => RevokedAt.HasValue;
    
    public bool IsActive(DateTime now)
    {
        now = DateTimeService.EnsureUtc(now);
        if (RevokedAt.HasValue)
        {
            return false;
        }
        
        if(ExpiresAt <= now)
        {
            RevokedAt = ExpiresAt;
            RevocationReason = Entities.RevocationReason.Timeout;
        }
        
        
        if (LastActivityAt.HasValue)
        {
            var inactivityThreshold = TimeSpan.FromMinutes(30); 
            var lastActivityAt = DateTimeService.EnsureUtc(LastActivityAt.Value);
            var revokedAt = lastActivityAt + inactivityThreshold;
            var isInactive = now - lastActivityAt > inactivityThreshold;
            
            RevokedAt = revokedAt;
            RevocationReason = Entities.RevocationReason.Inactivity;
            
            return !isInactive;
        }
        return false;
    }

}