using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;
using Hangfire;

namespace FieldService.Queue.Producers;

public abstract class AbstractPublishProducerWithRequest<TConsumer, TRequest> : AbstractHangfireProducer
    where TConsumer : IQueueConsumer<TRequest>
{
    protected AbstractPublishProducerWithRequest(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }

    public string Publish(Job<TRequest> job) =>
        BackgroundJobClient.Enqueue<TConsumer>(consumer => consumer.ExecuteAsync(job, CancellationToken.None));
}
