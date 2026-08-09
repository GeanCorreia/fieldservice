namespace FieldService.Queue.Interfaces;

public interface IQueueConsumer<in TRequest>
{
    Task ExecuteAsync(TRequest request);
}