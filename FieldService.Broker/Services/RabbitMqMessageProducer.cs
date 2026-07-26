using FieldService.Broker.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using FieldService.Broker.Message;

namespace FieldService.Broker.Services;

internal sealed class RabbitMqMessageProducer(IRabbitMqChannelFactory channelFactory) : IMessageProducer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task PublishAsync<T>(BrokerMessage<T> brokerMessage)
    {
        ArgumentNullException.ThrowIfNull(brokerMessage);
        ArgumentNullException.ThrowIfNull(brokerMessage.BrokerPublishContext);

        if (string.IsNullOrWhiteSpace(brokerMessage.BrokerPublishContext.Exchange))
            throw new InvalidOperationException("Publish exchange cannot be empty.");

        if (string.IsNullOrWhiteSpace(brokerMessage.BrokerPublishContext.RoutingKey))
            throw new InvalidOperationException("Publish routing key cannot be empty.");

        using var channel = channelFactory.CreateChannel();

        var basicProperties = brokerMessage.BrokerPublishContext.BasicProperties ?? channel.CreateBasicProperties();
        basicProperties.ContentType ??= "application/json";
        basicProperties.MessageId ??= brokerMessage.MessageId.ToString();
        basicProperties.Type ??= brokerMessage.MessageType;
        basicProperties.CorrelationId ??= brokerMessage.CorrelationId;
        basicProperties.Timestamp = new AmqpTimestamp(new DateTimeOffset(brokerMessage.CreatedAt).ToUnixTimeSeconds());
        basicProperties.Headers ??= new Dictionary<string, object>();
        basicProperties.Headers["tenant-id"] = brokerMessage.TenantId.ToString();
        basicProperties.Headers["schema-version"] = brokerMessage.SchemaVersion.ToString();

        var json = JsonSerializer.Serialize(brokerMessage, JsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(
            exchange: brokerMessage.BrokerPublishContext.Exchange,
            routingKey: brokerMessage.BrokerPublishContext.RoutingKey,
            basicProperties: basicProperties,
            body: body);

        return Task.CompletedTask;
    }
}
