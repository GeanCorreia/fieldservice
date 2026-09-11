using System.Diagnostics;
using System.Diagnostics.Metrics;
using Hangfire.Common;
using Hangfire.Server;

namespace FieldService.Queue.Filters;

public sealed class HangfireExecutionFilter : JobFilterAttribute, IServerFilter
{
    private const string StartedAtKey = "__hangfire_telemetry_started_at";
    private const string ActivityKey = "__hangfire_telemetry_activity";

    private static readonly ActivitySource ActivitySource = new("FieldService.Queue.Hangfire");
    private static readonly Meter Meter = new("FieldService.Queue.Hangfire");
    private static readonly Counter<long> SucceededJobs = Meter.CreateCounter<long>("hangfire.jobs.succeeded");
    private static readonly Counter<long> FailedJobs = Meter.CreateCounter<long>("hangfire.jobs.failed");
    private static readonly Histogram<double> DurationMs = Meter.CreateHistogram<double>("hangfire.jobs.duration.ms");

    public void OnPerforming(PerformingContext context)
    {
        var job = context.BackgroundJob.Job;
        var queue = context.GetJobParameter<string>("Queue") ?? "default";
        var correlationId = context.GetJobParameter<string>("CorrelationId");
        var tenantId = context.GetJobParameter<string>("TenantId");
        var isMultiTenant = bool.TryParse(context.GetJobParameter<string>("IsMultiTenant"), out var parsedIsMultiTenant) && parsedIsMultiTenant;
        var activity = ActivitySource.StartActivity("hangfire.job.execute", ActivityKind.Internal);
        var executionTraceId = activity?.TraceId.ToString() ?? string.Empty;
        context.SetJobParameter("ExecutionTraceId", executionTraceId);
        var recurringJobId = context.GetJobParameter<string>("RecurringJobId");
        
        var jobType = context.GetJobParameter<string>("JobType") ?? job.Type.Name;
        var hangfireId = context.BackgroundJob.Id; 
        var semanticId = $"{jobType}-{hangfireId}"; 

        activity?.SetTag("hangfire.job.id", semanticId);
        activity?.SetTag("hangfire.job.type", jobType);
        activity?.SetTag("hangfire.queue", queue);
        activity?.SetTag("app.correlation_id", correlationId);
        activity?.SetTag("app.trace_id", executionTraceId);
        activity?.SetTag("app.tenant_id", tenantId);
        activity?.SetTag("app.is_multi_tenant", isMultiTenant);
        activity?.SetTag("hangfire.recurring_job_id", recurringJobId);

        if (!string.IsNullOrWhiteSpace(correlationId))
            activity?.AddBaggage("correlation_id", correlationId);

        context.Items[StartedAtKey] = Stopwatch.GetTimestamp();
        context.Items[ActivityKey] = activity;
    }

    public void OnPerformed(PerformedContext context)
    {
        var startedAt = context.Items.TryGetValue(StartedAtKey, out var startedValue) && startedValue is long ticks
            ? ticks
            : Stopwatch.GetTimestamp();

        var duration = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        
        var queue = context.GetJobParameter<string>("Queue") ?? "default";
        var jobType = context.GetJobParameter<string>("JobType") ?? context.BackgroundJob.Job.Type.Name;

        var metricTags = new[]
        {
            new KeyValuePair<string, object?>("hangfire.queue", queue),
            new KeyValuePair<string, object?>("hangfire.job.type", jobType)
        };
        
        DurationMs.Record(duration, metricTags);

        var isSuccess = context.Exception is null && !context.Canceled;
        if (isSuccess)
            SucceededJobs.Add(1, metricTags);
        else
            FailedJobs.Add(1, metricTags);


        if (context.Items.TryGetValue(ActivityKey, out var activityObj) && activityObj is Activity activity)
        {
            activity.SetTag("hangfire.job.success", isSuccess);

            if (context.Exception is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, context.Exception.Message);
                activity.AddEvent(new ActivityEvent(
                    "exception",
                    tags: new ActivityTagsCollection
                    {
                        { "exception.type", context.Exception.GetType().FullName },
                        { "exception.message", context.Exception.Message },
                        { "exception.stacktrace", context.Exception.StackTrace }
                    }));
            }

            activity.Dispose();
        }
    }
}
