using FieldService.Observability.Types;

namespace FieldService.Authentication.Entities;

public class SessionActivity
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string JwtId { get; private set; }
    public string IpAddressHash { get; private set; }
    public string? UserAgentHash { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public RequestChannel Channel { get; private set; }
    public Guid RequestId { get; private set; }
    
    protected SessionActivity() { }
    
    public SessionActivity(
        Guid id, 
        Guid sessionId, 
        string jwtId,
        string ipAddressHash,
        DateTimeOffset timestamp, 
        RequestChannel channel, 
        Guid requestId,
        string? userAgentHash = null)
    {
        Id = id;
        SessionId = sessionId;
        JwtId = jwtId;
        IpAddressHash = ipAddressHash;
        UserAgentHash = userAgentHash;
        Timestamp = timestamp;
        Channel = channel;
        RequestId = requestId;
    }
}