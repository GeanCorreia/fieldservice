using System.Security.Claims;
using FieldService.Authentication.Interfaces;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;

namespace FieldService.SignalR.Services;

internal sealed class SignalRGetaway() : ISignalRGetaway
{
    public async Task<SignalRConnectionContext> ConnectAsync(string jwtToken, string connectionId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
