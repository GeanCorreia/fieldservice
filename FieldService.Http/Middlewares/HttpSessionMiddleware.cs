using FieldService.Authentication.Interfaces;
using FieldService.Authentication.SessionAttribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace FieldService.Http.Middlewares;

public class HttpSessionMiddleware
{
    private readonly RequestDelegate _next;

    public HttpSessionMiddleware(
        RequestDelegate next
        )
    {
        _next = next;
       
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISessionManager  sessionManager,
        IUserIdentityResolver  userIdentityResolver,
        IHostEnvironment environment
        )
    {
        
        
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }
        
        var cancellationToken = context.RequestAborted;
        
        await userIdentityResolver.ResolveUserAsync(cancellationToken);

        if (environment.IsDevelopment())
        {
            await _next(context);
            return;
        }
        
        
        var sessionAttribute = endpoint.Metadata.GetMetadata<ISessionOperation>();
        switch (sessionAttribute)
        {
            case SessionAtributes.TenantSelectionAttribute:
            case SessionAtributes.SessionCreationAttribute:
                await _next(context);
                return;

            default:
                await userIdentityResolver.ResolveSessionAsync(cancellationToken);
                await sessionManager.TouchAsync(cancellationToken);
                break;
        }
        
        await _next(context);
    }
}