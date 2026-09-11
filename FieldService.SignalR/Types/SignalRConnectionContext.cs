using System.Text.Json.Serialization;
using FieldService.Shared.Message;

namespace FieldService.SignalR.Types;

public sealed record SignalRConnectionContext : AbstractMessagePayload<SignalRConnectionContext>
{
    public string ConnectionId { get; init; }
    public Guid UserId { get; init; }
    public Guid SessionId { get; init; }
    public Guid TenantId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    [JsonConstructor]
    public SignalRConnectionContext(
        string connectionId,
        Guid userId,
        Guid sessionId,
        Guid tenantId,
        DateTimeOffset createdAt)
    {
        ConnectionId = connectionId;
        UserId = userId;
        SessionId = sessionId;
        TenantId = tenantId;
        CreatedAt = createdAt;
    }
}