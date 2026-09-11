using Microsoft.AspNetCore.Http;

namespace FieldService.SignalR.Interfaces;

public interface ISignalRConnectionRegistry
{
    Task OnConnectedAsync(string connectionId, HttpContext? httpContext, CancellationToken ct = default);
    Task OnDisconnectedAsync(string connectionId, CancellationToken ct = default);
}