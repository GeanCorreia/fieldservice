using FieldService.Broker.Configuration;
using FieldService.Broker.Interfaces;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldService.Broker.Jobs;

internal record BrokerCleanUpJob : Job
{
    public static readonly JobType JobType = "broker-outbox-message-cleanup";
    
    internal BrokerCleanUpJob()
        : base(new JobContext(JobType))
    {
    }
}

internal sealed class BrokerCleanUpJobHandler : IQueueConsumer
{
    
    private readonly ILogger<BrokerCleanUpJobHandler> _logger;
    private readonly IBrokerOutboxRepository _repository;
    private readonly int _retentionDays;

    
    public BrokerCleanUpJobHandler(
        ILogger<BrokerCleanUpJobHandler> logger,
        IBrokerOutboxRepository repository,
        IOptions<BrokerOutboxOptions> brokerOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        
        _retentionDays = brokerOptions?.Value.CleanupJobRetentionDays ?? throw new ArgumentNullException(nameof(brokerOptions));
    }
    public async Task ExecuteAsync(Job job, CancellationToken ct = default)
    {
        await _repository.DeleteProcessedBeforeAsync(DateTimeOffset.UtcNow.AddDays(-_retentionDays), ct);
    }
}

internal sealed class BrokerCleanUpJobProducer : AbstractScheduleRecurringProducer<BrokerCleanUpJobHandler>
{
    private readonly BrokerOutboxOptions _options;

    public BrokerCleanUpJobProducer(
        IRecurringJobManager recurringJobManager,
        IOptions<BrokerOutboxOptions> brokerOptions) 
        : base(recurringJobManager)
    {
        ArgumentNullException.ThrowIfNull(brokerOptions);
        _options = brokerOptions.Value;
    }

    protected override Job Job => new BrokerCleanUpJob();

    protected override string CronExpression => _options.CleanupJobCron;
}