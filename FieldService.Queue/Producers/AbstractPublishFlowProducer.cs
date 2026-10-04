using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;
using Hangfire;

namespace FieldService.Queue.Producers;

public abstract class AbstractPublishFlowProducer : AbstractHangfireProducer
{
    protected AbstractPublishFlowProducer(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }
    
    protected string EnqueueFirst<TConsumer>(Job job)
        where TConsumer : IQueueConsumer
    {
        return BackgroundJobClient.Enqueue<TConsumer>(
            consumer => consumer.ExecuteAsync(job, CancellationToken.None));
    }
    
    protected string ContinueWith<TConsumer>(string parentJobId, Job job)
        where TConsumer : IQueueConsumer
    {
        return BackgroundJobClient.ContinueJobWith<TConsumer>(
            parentJobId,
            consumer => consumer.ExecuteAsync(job, CancellationToken.None));
    }
}