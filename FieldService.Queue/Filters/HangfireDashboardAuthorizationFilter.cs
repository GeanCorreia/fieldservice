using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FieldService.Queue.Filters;

public sealed class HangfireDashboardAuthorizationFilter(
    IHostEnvironment environment,
    HangfireDashboardOptions options)
    : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = context.GetHttpContext();
        if (environment.IsDevelopment() && options.AllowAnonymousInDevelopment)
        {
            return true;
        }

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return false;

        if (string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
            return true;

        var authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        var result = authorizationService.AuthorizeAsync(httpContext.User, resource: null, options.AuthorizationPolicy)
            .GetAwaiter()
            .GetResult();

        return result.Succeeded;
    }
}
