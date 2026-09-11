using FieldService.Broker.Configuration;
using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;
using FieldService.Queue.Producers;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FieldService.Broker.Jobs;

public sealed class BrokerOutboxRetryProducer : AbstractScheduleRecurringProducer<JobBrokerOutboxRetryService>
{
    private readonly BrokerOutboxOptions _options;

    public BrokerOutboxRetryProducer(
        IOptions<BrokerOutboxOptions> options,
        IRecurringJobManager recurringJobManager)
        : base(recurringJobManager)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override Job Job => new BrokerOutboxRetryJob();
    protected override string CronExpression => _options.RetryJobCron;

}
