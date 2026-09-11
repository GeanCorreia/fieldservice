using FieldService.SignalR.Hubs;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;
using FieldService.Shared.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace FieldService.SignalR.Services;

internal sealed class SignalRConnectionRegistry(
    ISignalRPresenceRegistry presenceRegistry,
    ISignalRConnectionEventProducer connectionEventProducer,
    ISignalRRoomRegistry roomRegistry,
    IHubContext<SignalRHub> hubContext) : ISignalRConnectionRegistry
{
    public async Task OnConnectedAsync(string connectionId, HttpContext? httpContext, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));

        var connection = ResolveConnectionContext(connectionId, httpContext);
        
        await RegisterSignalRGroupsAsync(connection, ct);
        
        await presenceRegistry.RegisterAsync(
            connection.ConnectionId,
            connection.UserId,
            connection.SessionId,
            connection.TenantId,
            ct: ct);
        
        await roomRegistry.RestoreSessionRoomsAsync(connection.ConnectionId, connection.SessionId, ct);
        
        await connectionEventProducer.PublishAsync(connection, ct);
    }

    public async Task OnDisconnectedAsync(string connectionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));
        
        using var cleanupCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await roomRegistry.UnregisterFromAllRoomsAsync(connectionId, cleanupCts.Token);
        await presenceRegistry.UnregisterAsync(connectionId, cleanupCts.Token);
    }

    private async Task RegisterSignalRGroupsAsync(SignalRConnectionContext connection, CancellationToken ct)
    {
        var groupTasks = new[]
        {
            hubContext.Groups.AddToGroupAsync(connection.ConnectionId, $"user:{connection.UserId}", ct),
            hubContext.Groups.AddToGroupAsync(connection.ConnectionId, $"tenant:{connection.TenantId}", ct)
        };

        await Task.WhenAll(groupTasks);
    }

    private static SignalRConnectionContext ResolveConnectionContext(string connectionId, HttpContext? httpContext)
    {
        var principal = httpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Authenticated user is required for SignalR connection.");

        var userId = ReadRequiredGuidClaim(principal, ClaimsExtensions.UserId);
        var sessionId = ReadRequiredGuidClaim(principal, ClaimsExtensions.SessionId);
        var tenantId = ReadRequiredGuidClaim(principal, ClaimsExtensions.TenantId);

        return new SignalRConnectionContext(connectionId, userId, sessionId, tenantId, DateTimeOffset.UtcNow);
    }

    private static Guid ReadRequiredGuidClaim(System.Security.Claims.ClaimsPrincipal principal, string claimType)
    {
        var claim = principal.FindFirst(claimType)?.Value;
        if (!Guid.TryParse(claim, out var value) || value == default)
            throw new UnauthorizedAccessException($"Required claim '{claimType}' is missing or invalid.");

        return value;
    }
}