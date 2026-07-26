using FieldService.SignalR.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FieldService.SignalR.Hubs;

public sealed class SignalRHub(
    ISignalRConnectionRegistry connectionRegistry,
    ISignalRReceiver receiver) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await connectionRegistry.OnConnectedAsync(
            Context.ConnectionId,
            Context.GetHttpContext(),
            Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await connectionRegistry.OnDisconnectedAsync(Context.ConnectionId, Context.ConnectionAborted);
        await base.OnDisconnectedAsync(exception);
    }

    public Task AckDelivered(Guid notificationId, Guid deviceId, Guid userId)
    {
        return receiver.AckDeliveredAsync(notificationId, deviceId, userId, Context.ConnectionAborted);
    }

    public Task AckRead(Guid notificationId, Guid deviceId, Guid userId)
    {
        return receiver.AckReadAsync(notificationId, deviceId, userId, Context.ConnectionAborted);
    }
}
