using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;
using Hangfire;

namespace FieldService.Queue.Producers;

public class AbstractPublishFlowProducerWithRequest : AbstractHangfireProducer
{
    protected AbstractPublishFlowProducerWithRequest(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }
    
    protected string EnqueueFirst<TConsumer, TRequest>(Job<TRequest> job)
        where TConsumer : IQueueConsumer<TRequest>
    {
        return BackgroundJobClient.Enqueue<TConsumer>(
            consumer => consumer.ExecuteAsync(job, CancellationToken.None));
    }
    
    protected string ContinueWith<TConsumer, TRequest>(string parentJobId, Job<TRequest> job)
        where TConsumer : IQueueConsumer<TRequest>
    {
        return BackgroundJobClient.ContinueJobWith<TConsumer>(
            parentJobId,
            consumer => consumer.ExecuteAsync(job, CancellationToken.None));
    }
}