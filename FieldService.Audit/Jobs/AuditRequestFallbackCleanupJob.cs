using FieldService.Authentication.Interfaces;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FiledService.Audit.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FieldService.Audit.Jobs;

public record AuditRequestFallbackCleanupJob : Job
{
    public static readonly JobType JobType = "audit-request-fallback-cleanup";

    public AuditRequestFallbackCleanupJob()
        : base(new JobContext(JobType))
    {
    }
    
    public class AuditRequestFallbackCleanupJobHandler : IQueueConsumer    
    {
        private readonly ILogger<AuditRequestFallbackCleanupJobHandler> _logger;
        private readonly IAuditFallbackService _service;

        public AuditRequestFallbackCleanupJobHandler(
            ILogger<AuditRequestFallbackCleanupJobHandler> logger,
            IAuditFallbackService service
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [DisableConcurrentExecution(timeoutInSeconds: 3600)]
        public async Task ExecuteAsync(Job job, CancellationToken ct = default)
        {
            if(job.Context.Type != AuditRequestFallbackCleanupJob.JobType)
                throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");
            
            await _service.SaveFallbackSessionActivitiesAsync(ct);
            
        }
    }
    
    public class AuditRequestFallbackCleanupJobProducer : AbstractScheduleRecurringProducer<AuditRequestFallbackCleanupJobHandler>
    {
        public AuditRequestFallbackCleanupJobProducer(IRecurringJobManager recurringJobManager) : base(recurringJobManager)
        {
        }
        
        protected override Job Job => new AuditRequestFallbackCleanupJob();
        
        protected override string CronExpression => "14,29,44,59 * * * *";
    }
}