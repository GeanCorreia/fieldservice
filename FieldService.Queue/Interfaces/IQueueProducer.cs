namespace FieldService.Queue.Interfaces;

public interface IQueueProducer
{
    string Publish<TConsumer, TRequest>(TRequest request) 
        where TConsumer : IQueueConsumer<TRequest>;

    // Disparo com atraso (Delayed)
    string PublishDelayed<TConsumer, TRequest>(TRequest request, TimeSpan delay) 
        where TConsumer : IQueueConsumer<TRequest>;

    // Agendamento Recorrente (CRON)
    void ScheduleRecurring<TConsumer, TRequest>(string jobId, TRequest request, string cronExpression) 
        where TConsumer : IQueueConsumer<TRequest>;

    // Remover agendamento recorrente
    void RemoveRecurring(string jobId);
}