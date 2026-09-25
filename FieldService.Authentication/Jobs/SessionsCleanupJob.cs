using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FieldService.Authentication.Jobs;

public record SessionsCleanupJob : Job
{
    public static readonly JobType JobType = "sessions-cleanup";
    public SessionsCleanupJob()
        : base(new JobContext(JobType))
    {
    }
}

public class SessionsCleanupJobService : IQueueConsumer
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionsCleanupJobService> _logger;

    public SessionsCleanupJobService(
        ISessionRepository sessionRepository,
        ISessionService sessionService,
        ILogger<SessionsCleanupJobService> logger
    )
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task ExecuteAsync(Job job, CancellationToken ct = default)
    {
        if (job.Context.Type != SessionsCleanupJob.JobType)
            throw new InvalidOperationException($"Unexpected job type '{job.Context.Type}'.");

        var now = DateTimeOffset.UtcNow;

        const int pageSize = 100;
        var page = 1;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var inactivitySessions = await _sessionRepository.GetInactivitySessions(
                atTime: now,
                page: page,
                pageSize: pageSize,
                isDescending: false,
                ct: ct);

            var sessions = inactivitySessions.Data.ToList();
            if (sessions.Count == 0)
                break;

            foreach (var session in sessions)
            {
                try
                {
                    session.Revoke(now, RevocationReason.Inactivity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up session activities for session {SessionId}", session.Id);
                }
            }

            var revokedSessions = sessions.Where(s => s.IsRevoked).ToList();
            if (revokedSessions.Count > 0)
                await _sessionService.SaveSessionsAsync(revokedSessions, ct);

            if (page >= inactivitySessions.Pagination.TotalPages)
                break;

            page++;
        }
    }
}

public class SessionsCleanupProducer : AbstractScheduleRecurringProducer<SessionsCleanupJobService>
{
    public SessionsCleanupProducer(IRecurringJobManager recurringJobManager) : base(recurringJobManager)
    {
    }

    protected override Job Job => new SessionsCleanupJob();
    protected override string CronExpression => "9,24,39,54 * * * *";
}

