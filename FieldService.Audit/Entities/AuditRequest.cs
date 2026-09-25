using FieldService.Observability.Types;

namespace FieldService.Audit.Entities;

public class AuditRequest
{
    public Guid Id { get; private set; }
    public Guid UserId { get; set; }
    public Guid? SessionId { get; set; }
    public string JwtId { get; private set; }
    public string IpAddressHash { get; private set; }
    public string UserAgentHash { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public RequestChannel Channel { get; private set; }
    public string Resource { get; private set; }
    public bool Successful { get; private set; }
    public int StatusCode { get; private set; }
    protected AuditRequest() { }
    
    public AuditRequest(
        Guid id, 
        string jwtId,
        string ipAddressHash,
        DateTimeOffset timestamp, 
        RequestChannel channel, 
        string userAgentHash,
        string resource,
        bool successful,
        int statusCode,
        Guid userId,
        Guid? sessionId = null)
    {
        Id = id;
        JwtId = jwtId;
        IpAddressHash = ipAddressHash;
        UserAgentHash = userAgentHash;
        Timestamp = timestamp;
        Channel = channel;
        Resource = resource;
        Successful = successful;
        StatusCode = statusCode;
        UserId = userId;
        SessionId = sessionId;
    }
}