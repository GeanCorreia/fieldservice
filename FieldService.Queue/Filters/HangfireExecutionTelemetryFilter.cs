using System.Diagnostics;
using System.Diagnostics.Metrics;
using Hangfire.Common;
using Hangfire.Server;

namespace FieldService.Queue.Filters;

public sealed class HangfireExecutionTelemetryFilter : JobFilterAttribute, IServerFilter
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
        var activity = ActivitySource.StartActivity("hangfire.job.execute", ActivityKind.Internal);

        activity?.SetTag("hangfire.job.id", context.BackgroundJob.Id);
        activity?.SetTag("hangfire.job.type", job.Type.FullName);
        activity?.SetTag("hangfire.job.method", job.Method.Name);
        activity?.SetTag("hangfire.queue", queue);

        context.Items[StartedAtKey] = Stopwatch.GetTimestamp();
        context.Items[ActivityKey] = activity;
    }

    public void OnPerformed(PerformedContext context)
    {
        var queue = context.GetJobParameter<string>("Queue") ?? "default";
        var startedAt = context.Items.TryGetValue(StartedAtKey, out var startedValue) && startedValue is long ticks
            ? ticks
            : Stopwatch.GetTimestamp();

        var duration = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        DurationMs.Record(
            duration,
            new KeyValuePair<string, object?>("hangfire.queue", queue),
            new KeyValuePair<string, object?>("hangfire.job.type", context.BackgroundJob.Job.Type.FullName));

        var isSuccess = context.Exception is null && !context.Canceled;
        if (isSuccess)
            SucceededJobs.Add(1, new KeyValuePair<string, object?>("hangfire.queue", queue));
        else
            FailedJobs.Add(1, new KeyValuePair<string, object?>("hangfire.queue", queue));

        if (context.Items.TryGetValue(ActivityKey, out var activityObj) && activityObj is Activity activity)
        {
            activity.SetTag("hangfire.job.success", isSuccess);
            if (context.Exception is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, context.Exception.Message);
                activity.SetTag("exception.type", context.Exception.GetType().FullName);
                activity.SetTag("exception.message", context.Exception.Message);
            }

            activity.Dispose();
        }
    }
}
