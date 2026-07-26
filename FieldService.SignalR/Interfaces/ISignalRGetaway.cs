using FieldService.SignalR.Types;

namespace FieldService.SignalR.Interfaces;

public interface ISignalRGetaway
{
    Task<SignalRConnectionContext> ConnectAsync(string jwtToken, string connectionId, CancellationToken ct = default);
}
