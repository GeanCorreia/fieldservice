using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;
using Hangfire;

namespace FieldService.Queue.Producers;

public abstract class AbstractPublishProducer<TConsumer> : AbstractHangfireProducer
    where TConsumer : IQueueConsumer
{
    protected AbstractPublishProducer(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }

    public string Publish(Job job) =>
        BackgroundJobClient.Enqueue<TConsumer>(consumer => consumer.ExecuteAsync(job, CancellationToken.None));
}
