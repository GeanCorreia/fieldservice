# Broker (RabbitMQ) - Guia de Configuração e Uso

Este módulo centraliza:

- configuração e bootstrap da topologia (exchange/queue/bind) na inicialização;
- publicação de mensagens (`IMessageProducer`);
- registro de consumidores (`IMessageConsumer`);
- contrato base de mensagem (`BrokerMessage<T>`) para padronizar metadados e contexto de roteamento.

---

## 1. Exemplo de `appsettings.json`

```json
{
  "ConnectionStrings": {
    "RabbitMQ": "amqp://guest:guest@localhost:5672/"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "User": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "Topology": {
      "Exchanges": [
        {
          "Name": "fieldservice.exchange",
          "Type": "direct",
          "Durable": true,
          "AutoDelete": false,
          "Arguments": {
            "alternate-exchange": "fieldservice.ae"
          }
        }
      ],
      "Queues": [
        {
          "Name": "fieldservice.queue",
          "Durable": true,
          "Exclusive": false,
          "AutoDelete": false,
          "Arguments": {
            "x-dead-letter-exchange": "fieldservice.dlx"
          }
        }
      ],
      "Bindings": [
        {
          "Queue": "fieldservice.queue",
          "Exchange": "fieldservice.exchange",
          "RoutingKey": "customer.created"
        }
      ]
    }
  }
}
```

---

## 2. O que cada configuração faz

### ExchangeDeclare (`Topology:Exchanges`)

- **Name**: nome da exchange.
- **Type**: `direct`, `topic`, `fanout`, `headers`.
- **Durable**: persiste após restart do broker.
- **AutoDelete**: remove automaticamente quando não há mais uso.
- **Arguments**: argumentos avançados da exchange.

### QueueDeclare (`Topology:Queues`)

- **Name**: nome da fila.
- **Durable**: fila persistente.
- **Exclusive**: fila exclusiva da conexão atual.
- **AutoDelete**: remove quando não houver consumidores.
- **Arguments**: argumentos avançados (ex.: DLQ, TTL, max-length).

### QueueBind (`Topology:Bindings`)

- **Queue**: fila destino.
- **Exchange**: exchange origem.
- **RoutingKey**: chave de roteamento.

---

## 3. Como o bootstrap acontece

No `BrokerModule`:

1. lê `ConnectionStrings:RabbitMQ`;
2. cria `ConnectionFactory`;
3. lê `RabbitMQ:Topology:*`;
4. executa:
   - `ExchangeDeclare.Execute(...)`
   - `QueueDeclare.Execute(...)`
   - `QueueBind.Execute(...)`
5. registra serviços e contratos no DI.

---

## 4. Paralelismo implementado

### Conexão e channel

- `IRabbitMqPersistentConnection`: mantém conexão singleton reutilizável (thread-safe com lock).
- `IRabbitMqChannelFactory`: cria `IModel` por uso/operação.

### Producer

- cria channel por publish;
- serializa em JSON UTF-8;
- publica com `BasicPublish`.

### Consumer

- cria channel dedicado por registro;
- suporta `PrefetchCount` (controle de concorrência/throughput);
- `Ack/Nack` explícito com retry (`requeue=true`) em erro;
- modo global para escutar todas as filas da topologia.

---

## 5. Interfaces públicas

### `IMessageProducer`

```csharp
Task PublishAsync<T>(BrokerMessage<T> brokerMessage);
```

### `IMessageConsumer`

```csharp
Task RegisterAsync<T>(
    BrokerSubscribeContext context,
    Func<BrokerMessage<T>, CancellationToken, Task> handler,
    CancellationToken ct = default);

Task RegisterGlobalAsync(
    Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler,
    CancellationToken ct = default);
```

---

## 6. Contrato de mensagem e herança

`BrokerMessage<T>` é o contrato base para centralizar:

- metadados comuns (`MessageId`, `TenantId`, `MessageType`, `SchemaVersion`, etc.);
- payload tipado;
- `BrokerPublishContext` (exchange/routing key/propriedades).

Exemplo de mensagem específica:

```csharp
using FieldService.Domain.ValueObjects;
using FieldService.Broker;
using FieldService.Broker.Message;

public sealed record CustomerCreatedPayload(Guid CustomerId, string Name, string Email);

public sealed record CustomerCreatedMessage(
    Guid MessageId,
    Guid TenantId,
    DateTime OccurredAtUtc,
    string? CorrelationId,
    CustomerCreatedPayload Payload)
    : BrokerMessage<CustomerCreatedPayload>(
        MessageId,
        TenantId,
        "customer.created",
        OccurredAtUtc,
        CorrelationId,
        new Version(1, 0, 0),
        Payload,
        new BrokerPublishContext(
            Exchange: "fieldservice.exchange",
            RoutingKey: "customer.created"));
```

Com isso, a aplicação só instancia `CustomerCreatedMessage` e publica via `IMessageProducer`, mantendo o `BrokerContext` centralizado e consistente.
