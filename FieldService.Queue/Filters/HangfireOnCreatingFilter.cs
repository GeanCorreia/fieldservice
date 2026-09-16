using System.Diagnostics;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Tags;
using QueueJob = FieldService.Queue.Types.Job;

namespace FieldService.Queue.Filters;

public sealed class HangfireOnCreatingFilter : JobFilterAttribute, IClientFilter
{
    public void OnCreating(CreatingContext filterContext)
    {
        var job = GetJobArgument(filterContext.Job.Args);
        if (job is null)
            return;
        
        filterContext.SetJobParameter("JobType", job.Context.Type.ToString());
        filterContext.SetJobParameter("CorrelationId", job.Context.CorrelationId);
        filterContext.SetJobParameter("TenantId", job.Context.TenantId?.ToString() ?? string.Empty);
        filterContext.SetJobParameter("IsMultiTenant", (!job.Context.TenantId.HasValue).ToString());
        filterContext.SetJobParameter("TraceId", Activity.Current?.TraceId.ToString() ?? string.Empty);
    }

    public void OnCreated(CreatedContext filterContext)
    {
        if (filterContext.BackgroundJob is null)
            return;

        var job = GetJobArgument(filterContext.Job.Args);
        if (job is null)
            return;
        
        var jobTypeTag = $"Job:{job.Context.Type}";
        
        var hangfireId = filterContext.BackgroundJob.Id; 
       
        var tenantTag = job.Context.TenantId.HasValue
            ? $"Tenant:{job.Context.TenantId}"
            : "Tenant:Multi";
        
        hangfireId.AddTags(
            jobTypeTag,
            tenantTag
        );
    }

    private static QueueJob? GetJobArgument(IReadOnlyList<object> args) =>
        args.OfType<QueueJob>().FirstOrDefault();
}
