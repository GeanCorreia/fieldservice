using FieldService.SignalR.Types;
using Microsoft.AspNetCore.Http;

namespace FieldService.SignalR.Interfaces;

internal interface ISignalRConnectionManager 
{
    Task OnConnectedAsync(SignalRConnectionContext context, CancellationToken ct = default);
    Task OnDisconnectedAsync(string connectionId, CancellationToken ct = default);

    
    
}