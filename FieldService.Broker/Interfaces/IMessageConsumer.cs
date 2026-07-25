using FieldService.Broker.Message;

namespace FieldService.Broker.Interfaces;

public interface IMessageConsumer
{
    Task RegisterAsync<T>(
        BrokerSubscribeContext context,
        Func<BrokerMessage<T>, CancellationToken, Task> handler,
        CancellationToken ct = default);

    Task RegisterGlobalAsync(
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler,
        CancellationToken ct = default);
}
