using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;
using Hangfire;

namespace FieldService.Queue.Producers;

public abstract class AbstractScheduleRecurringProducer<TConsumer> where TConsumer : IQueueConsumer
{
    private readonly IRecurringJobManager _recurringJobManager;

    protected AbstractScheduleRecurringProducer(IRecurringJobManager recurringJobManager)
    {
        _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
    }

    protected abstract Job Job { get; }

    protected abstract string CronExpression { get; }

    public void ScheduleRecurring()
    {
        _recurringJobManager.AddOrUpdate<TConsumer>(
            Job.JobId,
            service => service.ExecuteAsync(Job, CancellationToken.None),
            CronExpression);
    }
}