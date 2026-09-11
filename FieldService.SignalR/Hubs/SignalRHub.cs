using FieldService.Authentication.SessionAttribute;
using FieldService.SignalR.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FieldService.SignalR.Hubs;

[SessionAtributes.SignalRAttribute]
public sealed class SignalRHub(
    ISignalRConnectionRegistry connectionRegistry) : Hub
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
}
