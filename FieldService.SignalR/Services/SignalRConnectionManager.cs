using FieldService.Shared.Services;
using FieldService.SignalR.Hubs;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace FieldService.SignalR.Services;

internal sealed class SignalRConnectionManager(
    IPresenceRegistry presenceRegistry,
    ISignalRConnectionEventProducer connectionEventProducer,
    IHubContext<SignalRHub> hubContext) : ISignalRConnectionManager
{
    public async Task OnConnectedAsync(SignalRConnectionContext context, CancellationToken ct = default)
    {
        
        var previousConnection = await presenceRegistry.GetConnectionContextBySessionIdAsync(
            context.SessionId, 
            ct);


        if (previousConnection is not null && previousConnection.ConnectionId != context.ConnectionId)
        {
            await UnregisterSignalRGroupsAsync(previousConnection, ct);
            await presenceRegistry.UnregisterConnectionAsync(previousConnection.ConnectionId, ct);
        }
        
        await RegisterSignalRGroupsAsync(context, ct);
        
        await presenceRegistry.RegisterAsync(context, ct);
        
        await connectionEventProducer.PublishAsync(context, ct);
    }

    public async Task OnDisconnectedAsync(string connectionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId is required.", nameof(connectionId));

        using var cleanupCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        var connection = await presenceRegistry.GetConnectionContextByConnectionIdAsync(
            connectionId, 
            cleanupCts.Token);

        if (connection is not null)
        {
            await UnregisterSignalRGroupsAsync(connection, cleanupCts.Token);
        }

        await presenceRegistry.UnregisterConnectionAsync(connectionId, cleanupCts.Token);
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

    private async Task UnregisterSignalRGroupsAsync(SignalRConnectionContext connection, CancellationToken ct)
    {
        var groupTasks = new[]
        {
            hubContext.Groups.RemoveFromGroupAsync(connection.ConnectionId, $"user:{connection.UserId}", ct),
            hubContext.Groups.RemoveFromGroupAsync(connection.ConnectionId, $"tenant:{connection.TenantId}", ct)
        };

        await Task.WhenAll(groupTasks);
    }

    

    
}