using FieldService.Shared.Services;
using FieldService.Shared.Types;

namespace FieldService.Shared.Types;

public sealed class RequestContext
{
    public Guid RequestId { get; init; }
    public string TraceId { get; set; }
    public RequestChannel Channel { get; init; }
    public string IpAddressHash { get; init; }
    public DateTime Timestamp { get; init; }
    public string? UserAgentHash { get; init; }
    public Guid? SessionId { get; set; }
    public Guid? TenantId { get; set; }

    private RequestContext(
        Guid requestId,
        string traceId,
        RequestChannel channel,
        string ipAddressHash,
        DateTime timestamp,
        string? userAgentHash = null,
        Guid? sessionId = null,
        Guid? tenantId = null)
    {
        RequestId = requestId;
        TraceId = traceId;
        Channel = channel;
        IpAddressHash = ipAddressHash;
        Timestamp = timestamp;
        UserAgentHash = userAgentHash;
        SessionId = sessionId;
        TenantId = tenantId;
    }

    public static RequestContext Create(
        RequestChannel channel,
        string ipAddress,
        string traceId,
        string? userAgent = null,
        Guid? sessionId = null,
        Guid? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        var hashService = new HashService();

        var ipAddressHash = hashService.Generate(ipAddress);
        var userAgentHash = string.IsNullOrWhiteSpace(userAgent) 
            ? null 
            : hashService.Generate(userAgent);
        var timestamp = DateTimeService.GetNow();

        return new RequestContext(
            Guid.NewGuid(),
            traceId,
            channel,
            ipAddressHash,
            timestamp,
            userAgentHash,
            sessionId,
            tenantId);
    }
}