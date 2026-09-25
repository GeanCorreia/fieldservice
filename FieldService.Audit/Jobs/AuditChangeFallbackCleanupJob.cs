using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FiledService.Audit.Interfaces;
using Hangfire;

namespace FieldService.Audit.Jobs;

public record AuditChangeFallbackCleanupJob : Job
{
    public static readonly JobType JobType = "audit-change-fallback-cleanup";

    public AuditChangeFallbackCleanupJob() : base(new JobContext(JobType))
    {
    }

    public class AuditChangeFallbackCleanupJobHandler(IAuditFallbackService service) : IQueueConsumer
    {
        private readonly IAuditFallbackService _service = service ?? throw new ArgumentNullException(nameof(service));

        [DisableConcurrentExecution(timeoutInSeconds: 3600)]
        public async Task ExecuteAsync(Job job, CancellationToken ct = default)
        {
            if (job.Context.Type != AuditChangeFallbackCleanupJob.JobType)
                throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

            await _service.SaveFallbackAuditChangesAsync(ct);
        }
    }

    public class AuditChangeFallbackCleanupJobProducer : AbstractScheduleRecurringProducer<AuditChangeFallbackCleanupJobHandler>
    {
        public AuditChangeFallbackCleanupJobProducer(IRecurringJobManager recurringJobManager) : base(recurringJobManager)
        {
        }

        protected override Job Job => new AuditChangeFallbackCleanupJob();

        protected override string CronExpression => "13,28,43,58 * * * *";
    }
}

