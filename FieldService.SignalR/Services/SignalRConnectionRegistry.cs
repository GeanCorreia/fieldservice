using FieldService.SignalR.Hubs;
using FieldService.SignalR.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace FieldService.SignalR.Services;

internal sealed class SignalRConnectionRegistry(
    ISignalRGetaway signalRGetaway,
    ISignalRPresenceRegistry presenceRegistry,
    ISignalRConnectionEventProducer connectionEventProducer,
    IHubContext<SignalRHub> hubContext) : ISignalRConnectionRegistry
{
    public async Task OnConnectedAsync(string connectionId, HttpContext? httpContext, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));

        var token = ResolveToken(httpContext);
        var connection = await signalRGetaway.ConnectAsync(token, connectionId, ct);

        await hubContext.Groups.AddToGroupAsync(connectionId, $"user:{connection.UserId}", ct);
        await hubContext.Groups.AddToGroupAsync(connectionId, $"device:{connection.DeviceId}", ct);
        await presenceRegistry.RegisterAsync(connectionId, connection.UserId, connection.DeviceId, ct);

        foreach (var tenantId in connection.TenantIds)
        {
            await hubContext.Groups.AddToGroupAsync(connectionId, $"tenant:{tenantId}", ct);
            await presenceRegistry.SubscribeTenantAsync(connectionId, tenantId, ct);
        }

        await connectionEventProducer.PublishConnectedAsync(connection, ct);
    }

    public Task OnDisconnectedAsync(string connectionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));

        return presenceRegistry.UnregisterAsync(connectionId, ct);
    }

    public async Task SubscribeTenantAsync(string connectionId, Guid tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        await hubContext.Groups.AddToGroupAsync(connectionId, $"tenant:{tenantId}", ct);
        await presenceRegistry.SubscribeTenantAsync(connectionId, tenantId, ct);
    }

    public async Task UnsubscribeTenantAsync(string connectionId, Guid tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        await hubContext.Groups.RemoveFromGroupAsync(connectionId, $"tenant:{tenantId}", ct);
        await presenceRegistry.UnsubscribeTenantAsync(connectionId, tenantId, ct);
    }

    private static string ResolveToken(HttpContext? httpContext)
    {
        var token = httpContext?.Request.Query["access_token"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(token))
            return token;

        var authHeader = httpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader) &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        throw new UnauthorizedAccessException("JWT token was not provided.");
    }
}
