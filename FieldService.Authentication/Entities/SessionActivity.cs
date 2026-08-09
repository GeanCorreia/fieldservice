using FieldService.Shared.Types;
using FieldService.Shared.Services;

namespace FieldService.Authentication.Entities;

public class SessionActivity
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string JwtId { get; private set; }
    public string IpAddressHash { get; private set; }
    public string? UserAgentHash { get; private set; }
    public DateTime Timestamp { get; private set; }
    public RequestChannel Channel { get; private set; }
    public Guid RequestId { get; private set; }
    
    protected SessionActivity() { }
    
    public SessionActivity(
        Guid id, 
        Guid sessionId, 
        string jwtId,
        string ipAddressHash,
        DateTime timestamp, 
        RequestChannel channel, 
        Guid requestId,
        string? userAgentHash = null)
    {
        Id = id;
        SessionId = sessionId;
        JwtId = jwtId;
        IpAddressHash = ipAddressHash;
        UserAgentHash = userAgentHash;
        Timestamp = DateTimeService.EnsureUtc(timestamp);
        Channel = channel;
        RequestId = requestId;
    }
}