using FieldService.Queue.Types;

namespace FieldService.Broker.Jobs;

public sealed record BrokerOutboxRetryJob : Job
{
    public static readonly JobType JobType = "broker-outbox-retry";

    public BrokerOutboxRetryJob()
        : base(JobType, new JobContext(JobType))
    {
    }
}
