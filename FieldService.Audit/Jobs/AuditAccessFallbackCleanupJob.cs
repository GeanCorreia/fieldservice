using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FiledService.Audit.Interfaces;
using Hangfire;

namespace FieldService.Audit.Jobs;

public record AuditAccessFallbackCleanupJob : Job
{
    public static readonly JobType JobType = "audit-access-fallback-cleanup";

    public AuditAccessFallbackCleanupJob() : base(new JobContext(JobType))
    {
    }

    public class AuditAccessFallbackCleanupJobHandler(IAuditFallbackService service) : IQueueConsumer
    {
        private readonly IAuditFallbackService _service = service ?? throw new ArgumentNullException(nameof(service));

        [DisableConcurrentExecution(timeoutInSeconds: 3600)]
        public async Task ExecuteAsync(Job job, CancellationToken ct = default)
        {
            if (job.Context.Type != AuditAccessFallbackCleanupJob.JobType)
                throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

            await _service.SaveFallbackAuditAccessesAsync(ct);
        }
    }

    public class AuditAccessFallbackCleanupJobProducer : AbstractScheduleRecurringProducer<AuditAccessFallbackCleanupJobHandler>
    {
        public AuditAccessFallbackCleanupJobProducer(IRecurringJobManager recurringJobManager) : base(recurringJobManager)
        {
        }

        protected override Job Job => new AuditAccessFallbackCleanupJob();

        protected override string CronExpression => "12,27,42,57 * * * *";
    }
}

