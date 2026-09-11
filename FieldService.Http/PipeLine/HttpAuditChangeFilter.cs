using FiledService.Audit.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FieldService.Http.PipeLine;

public sealed class HttpAuditChangeFilter(IAuditChangeService auditChangeService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        try
        {
            await next();
        }
        finally
        {
            await auditChangeService.AuditChanges(context.HttpContext.RequestAborted);
        }
    }
}
