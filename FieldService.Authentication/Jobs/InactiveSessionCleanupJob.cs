using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldService.Authentication.Jobs;

public sealed record InactiveSessionCleanupJob : Job
{
    public static readonly JobType JobType = "inactive-session-cleanup";

    public InactiveSessionCleanupJob()
        : base(JobType, new JobContext(JobType))
    {
    }
}

public sealed class InactiveSessionCleanupJobService : IQueueConsumer
{
    private readonly ISessionPersistenceService _sessionPersistenceService;
    private readonly ILogger<InactiveSessionCleanupJobService> _logger;


    public InactiveSessionCleanupJobService(
        ISessionPersistenceService sessionPersistenceService,
        ILogger<InactiveSessionCleanupJobService> logger

    )
    {
        _sessionPersistenceService = sessionPersistenceService ??
                                     throw new ArgumentNullException(nameof(sessionPersistenceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));


    }

    public async Task ExecuteAsync(Job job, CancellationToken ct = default)
    {
        if (job.Type != InactiveSessionCleanupJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Type}'.");

        await _sessionPersistenceService.InactiveCleanupAsync(ct);


    }
}

public sealed class InactiveSessionCleanupProducer : AbstractScheduleRecurringProducer<InactiveSessionCleanupJobService>
{
    public InactiveSessionCleanupProducer(IRecurringJobManager recurringJobManager) : base(recurringJobManager)
    {
    }

    protected override Job Job => new InactiveSessionCleanupJob();
    protected override string CronExpression => "13 * * * *";
}

