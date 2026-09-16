using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;
using Hangfire;

namespace FieldService.Queue.Producers;

public abstract class AbstractPublishDelayedProducer<TConsumer> : AbstractHangfireProducer
    where TConsumer : IQueueConsumer
{
    protected AbstractPublishDelayedProducer(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }

    public string PublishDelayed(Job job, TimeSpan delay) =>
        BackgroundJobClient.Schedule<TConsumer>(consumer => consumer.ExecuteAsync(job, CancellationToken.None), delay);
}


