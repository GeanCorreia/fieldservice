using FieldService.Authentication.Events;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Entities;

public class UserAuthentication
{
    public Guid UserId { get; private set; }
    public string ExternalId { get; private set; }
    
    private ICollection<UserTenantEvent> _events = new List<UserTenantEvent>();
    public IReadOnlyCollection<UserTenantAuthentication> Tenants => _events
        .GroupBy(e => e.TenantId)
        .Select(g => new 
        {
            TenantId = g.Key,
            LatestEvent = g.OrderByDescending(e => e.CreatedAt).First()
        })
        .Where(x => x.LatestEvent.Status == TenantMembershipStatus.Active)
        .OrderByDescending(x => x.LatestEvent.CreatedAt)
        .Select(x => new UserTenantAuthentication(
            UserId,
            x.TenantId,
            x.LatestEvent.TenantName
        ))
        .ToList()
        .AsReadOnly();

    public AuthenticationProvider Provider { get; private set; }

    protected UserAuthentication() { }

    public UserAuthentication(
        Guid userId, 
        string externalId, 
        AuthenticationProvider provider,
        ICollection<UserTenantAuthentication> tenants)
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
        _events = tenants.Select(t => UserTenantEvent.Active(
                userId,
                t.TenantId,
                t.Name,
                userId))
            .ToList();
    }
}