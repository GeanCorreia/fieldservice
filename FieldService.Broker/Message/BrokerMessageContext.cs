namespace FieldService.Broker.Message;

public sealed record BrokerPublishContext(
    string EntityName,                                    // Nome do Tópico ou Fila no Service Bus
    string? PartitionKey = null,                          // Chave para garantir ordenação na mesma partição
    string? SessionId = null,                             // ID para agrupamento/sessões (FIFO)
    string? ReplyTo = null,                               // Fila/Tópico para respostas (Request/Reply pattern)
    TimeSpan? TimeToLive = null,                          // Tempo de vida (TTL) antes da mensagem ir pra Dead-Letter
    DateTimeOffset? ScheduledEnqueueTimeUtc = null,       // Data/hora UTC em que a mensagem ficará visível/disponível
    IDictionary<string, object?>? Headers = null);


public sealed record BrokerSubscribeContext(
    string EntityName,
    string SubscriptionName,                              // Nome da Assinatura dentro do Tópico (antigo EndpointName)
    ushort PrefetchCount = 20,                            // Quantidade de mensagens trazidas para memória antes de processar
    int? ConcurrentMessageLimit = 1,                      // MaxConcurrentCalls no ServiceBusProcessor
    bool EnableSessions = false,                          // Ativa escuta com controle de sessão (FIFO)
    TimeSpan? AutoRenewTimeout = null,                    // Tempo para renovação automática do Lock da mensagem (MaxAutoLockRenewalDuration)
    int BatchSize = 10,                                   // Tamanho máximo do lote para consumo em batch
    TimeSpan? BatchTimeout = null,                        // Tempo máximo de espera para formar um lote antes de processar
    TimeSpan? DuplicateDetection = null);                 // Tempo para detecção de mensagens duplicadas (Duplicate Detection Window)
