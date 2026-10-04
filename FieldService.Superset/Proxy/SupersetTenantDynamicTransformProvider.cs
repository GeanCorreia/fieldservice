using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Shared.Services;
using FieldService.Superset.Attributes;
using FieldService.Superset.Configuration;
using FieldService.Superset.Cqrs.Queries.GetSupersetUser;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Utils;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace FieldService.Superset.Proxy;

public class SupersetTenantDynamicTransformProvider : ITransformProvider
{
    private readonly string _routeId;

    public SupersetTenantDynamicTransformProvider(IOptions<SupersetOptions> supersetOptions)
    {
        ArgumentNullException.ThrowIfNull(supersetOptions);
        _routeId = supersetOptions.Value.RouteId ?? throw new ArgumentNullException(nameof(supersetOptions.Value.RouteId));
    }

    public void ValidateRoute(TransformRouteValidationContext context) { }

    public void ValidateCluster(TransformClusterValidationContext context) { }

    public void Apply(TransformBuilderContext context)
    {
        if (context.Route.RouteId == _routeId)
        {
            context.AddRequestTransform(async transformContext =>
            {
                var httpContext = transformContext.HttpContext;
                var principal = httpContext.User;
                var tenantId = ClaimsResolver.GetTenantId(principal);
                var userId = ClaimsResolver.GetUserId(principal);
                
                var supersetService = httpContext.RequestServices.GetRequiredService<ISupersetTenantService>();
                var mediator = httpContext.RequestServices.GetRequiredService<IMediator>();
                
                var supersetTenant = await supersetService
                    .GetSupersetTenantByIdAsync(tenantId, httpContext.RequestAborted);
                
                if (supersetTenant == null)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                if (string.IsNullOrWhiteSpace(supersetTenant.Container.FqdnUrl))
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                
                var user = await mediator.Send(new GetSupersetUserQuery(userId, tenantId), httpContext.RequestAborted);
                if (user == null)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
                
                var userPermissions = user.Permissions;
                var isStandAloneUser = !userPermissions.Contains(SupersetPermissions.AdminPermission);
                
                var targetUri = new Uri(supersetTenant.Container.FqdnUrl);
                var pathAndQuery = httpContext.Request.GetEncodedPathAndQuery();

                if (isStandAloneUser)
                {
                    pathAndQuery = QueryHelpers.AddQueryString(pathAndQuery, "standalone", "1");
                }

                transformContext.ProxyRequest.RequestUri = new Uri(targetUri, pathAndQuery);
                
                transformContext.ProxyRequest.Headers.Host = targetUri.Host;
                
                var username = SupersetUsernameResolver.ResolveUsername(userId, tenantId);
                if (!string.IsNullOrEmpty(username))
                {
                    transformContext.ProxyRequest.Headers.Remove(SupersetProxyHeaders.RemoteUserHeaderName);
                    transformContext.ProxyRequest.Headers.Add(SupersetProxyHeaders.RemoteUserHeaderName, username);
                }
            });
        }
    }
}