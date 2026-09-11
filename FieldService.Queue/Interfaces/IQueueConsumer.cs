using FieldService.Queue.Types;

namespace FieldService.Queue.Interfaces;

public interface IQueueConsumer
{
    Task ExecuteAsync(Job job, CancellationToken ct = default);
}

public interface IQueueConsumer<TRequest>
{
    Task ExecuteAsync(Job<TRequest> job, CancellationToken ct = default);
}