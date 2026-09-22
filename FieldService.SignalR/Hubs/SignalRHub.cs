using FieldService.Authentication.SessionAttribute;
using FieldService.Shared.Services;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace FieldService.SignalR.Hubs;

[SessionAtributes.SignalRAttribute]
internal sealed class SignalRHub(
    ISignalRConnectionManager connectionManager,
    IDomainRoomManager domainRoomManager) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var context = GetConnectionContext();
        
        await connectionManager.OnConnectedAsync(
            context,
            Context.ConnectionAborted);
        
        await domainRoomManager.OnConnectedAsync(
            context, 
            Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await connectionManager.OnDisconnectedAsync(Context.ConnectionId, Context.ConnectionAborted);
        await domainRoomManager.OnDisconnectedAsync(Context.ConnectionId, Context.ConnectionAborted);
        await base.OnDisconnectedAsync(exception);
    }
    
    private SignalRConnectionContext GetConnectionContext()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }
        return ResolveConnectionContext(Context.ConnectionId, httpContext);
    }
    
    private static SignalRConnectionContext ResolveConnectionContext(string connectionId, HttpContext? httpContext)
    {
        var principal = httpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Authenticated user is required for SignalR connection.");

        var userId = ClaimsResolver.GetUserId(principal);
        var sessionId = ClaimsResolver.GetSessionId(principal);
        var tenantId = ClaimsResolver.GetTenantId(principal);

        return new SignalRConnectionContext(connectionId, userId, sessionId, tenantId, DateTimeOffset.UtcNow);
    }
}
