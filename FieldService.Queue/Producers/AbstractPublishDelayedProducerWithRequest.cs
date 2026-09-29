using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;
using Hangfire;

namespace FieldService.Queue.Producers;

public abstract class AbstractPublishDelayedProducerWithRequest<TConsumer, TRequest> : AbstractHangfireProducer
    where TConsumer : IQueueConsumer<TRequest>
{
    protected AbstractPublishDelayedProducerWithRequest(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }

    public string PublishDelayed(Job<TRequest> job, TimeSpan delay) =>
        BackgroundJobClient.Schedule<TConsumer>(consumer => consumer.ExecuteAsync(job, CancellationToken.None), delay);
}
