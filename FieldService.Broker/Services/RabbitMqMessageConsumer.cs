using FieldService.Broker.Configuration;
using FieldService.Broker.Interfaces;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using FieldService.Broker.Message;

namespace FieldService.Broker.Services;

internal sealed class RabbitMqMessageConsumer(
    IRabbitMqChannelFactory channelFactory,
    IConfiguration configuration,
    IMessageProcessingLock messageProcessingLock) : IMessageConsumer, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ProcessingLockTtl = TimeSpan.FromHours(24);
    private readonly ConcurrentBag<IModel> _channels = [];
    private bool _disposed;

    public Task RegisterAsync<T>(
        BrokerSubscribeContext context,
        Func<BrokerMessage<T>, CancellationToken, Task> handler,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(handler);
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.Queue))
            throw new InvalidOperationException("Subscribe queue cannot be empty.");
        if (string.IsNullOrWhiteSpace(context.Exchange))
            throw new InvalidOperationException("Subscribe exchange cannot be empty.");

        ThrowIfDisposed();

        var channel = channelFactory.CreateChannel();
        _channels.Add(channel);

        if (context.PrefetchCount > 0)
            channel.BasicQos(0, context.PrefetchCount, false);

        channel.QueueBind(
            queue: context.Queue,
            exchange: context.Exchange,
            routingKey: context.RoutingKey,
            arguments: context.BindingArguments);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var message = JsonSerializer.Deserialize<BrokerMessage<T>>(json, JsonOptions)
                              ?? throw new InvalidOperationException("Failed to deserialize broker message.");

                var messageLockKey = $"broker:processed:{message.TenantId}:{message.MessageId}";
                var acquired = await messageProcessingLock.TryAcquireAsync(messageLockKey, ProcessingLockTtl, ct);
                if (!acquired)
                {
                    if (!context.AutoAck)
                        channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                await handler(message, ct);

                if (!context.AutoAck)
                    channel.BasicAck(ea.DeliveryTag, false);
            }
            catch
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var message = JsonSerializer.Deserialize<BrokerMessage<T>>(json, JsonOptions);
                if (message is not null)
                {
                    var messageLockKey = $"broker:processed:{message.TenantId}:{message.MessageId}";
                    await messageProcessingLock.ReleaseAsync(messageLockKey, ct);
                }

                if (!context.AutoAck)
                    channel.BasicNack(ea.DeliveryTag, false, true);
                throw;
            }
        };

        channel.BasicConsume(
            queue: context.Queue,
            autoAck: context.AutoAck,
            consumerTag: context.ConsumerTag,
            noLocal: context.NoLocal,
            exclusive: context.Exclusive,
            arguments: context.ConsumerArguments,
            consumer: consumer);

        return Task.CompletedTask;
    }

    public Task RegisterGlobalAsync(
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ct.ThrowIfCancellationRequested();

        ThrowIfDisposed();

        var queues = configuration.GetSection("RabbitMQ:Topology:Queues").Get<List<QueueDeclaration>>() ?? [];
        if (queues.Count == 0)
            throw new InvalidOperationException("RabbitMQ:Topology:Queues is empty. Global consumer has no queues to listen.");

        foreach (var queue in queues)
        {
            if (string.IsNullOrWhiteSpace(queue.Name))
                throw new InvalidOperationException("RabbitMQ topology queue name cannot be empty.");

            var channel = channelFactory.CreateChannel();
            _channels.Add(channel);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    await handler(ea.Body, ct);
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch
                {
                    channel.BasicNack(ea.DeliveryTag, false, true);
                    throw;
                }
            };

            channel.BasicConsume(
                queue: queue.Name,
                autoAck: false,
                consumer: consumer);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        while (_channels.TryTake(out var channel))
            channel.Dispose();

        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqMessageConsumer));
    }
}
