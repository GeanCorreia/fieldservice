# FieldService.Queue — Guia de Uso

## Objetivo do módulo

O `FieldService.Queue` encapsula o Hangfire atrás de contratos de domínio (`IQueueProducer`, `IQueueConsumer`, `Job`, `Job<TPayload>`), para que os demais módulos publiquem e consumam jobs sem acoplamento direto ao Hangfire.

---

## Conceitos principais

- **`JobType`**: identifica semanticamente o tipo do job.
- **`JobContext`**: contexto de transporte/execução:
  - `Type`
  - `CorrelationId`
  - `TenantId` (opcional, pode ser multi-tenant quando `null`)
- **`Job`**: envelope sem payload (execuções de manutenção/rotina).
- **`Job<TPayload>`**: envelope com payload tipado (execuções orientadas a dados).

---

## Contratos públicos

### Consumer sem payload

```csharp
public sealed class CleanupExpiredSessionsConsumer : IQueueConsumer
{
    public Task ExecuteAsync(Job job)
    {
        // job.Context.CorrelationId / TenantId / Type
        return Task.CompletedTask;
    }
}
```

### Consumer com payload

```csharp
public sealed class ProcessInvoiceConsumer : IQueueConsumer<ProcessInvoiceRequest>
{
    public Task ExecuteAsync(Job<ProcessInvoiceRequest> job)
    {
        var request = job.Payload;
        var correlationId = job.Context.CorrelationId;
        return Task.CompletedTask;
    }
}
```

### Producer

```csharp
public sealed class BillingAppService(IQueueProducer queueProducer)
{
    public string EnqueueInvoice(ProcessInvoiceRequest request, Guid tenantId)
    {
        var job = Job<ProcessInvoiceRequest>.Create(
            payload: request,
            tenantId: tenantId,
            correlationId: Guid.NewGuid().ToString("N"));

        return queueProducer.Publish<ProcessInvoiceConsumer, ProcessInvoiceRequest>(job);
    }
}
```

### Exemplo completo (Payload + Handler + Consumer)

```csharp
using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;

public record RelatorioPrestadorPayload(Guid PrestadorId);

public sealed class RelatorioPrestadorConsumer : IQueueConsumer<RelatorioPrestadorPayload>
{
    public async Task ExecuteAsync(Job<RelatorioPrestadorPayload> job)
    {
        var prestadorId = job.Payload.PrestadorId;
        var tenantId = job.Context.TenantId;
        var correlationId = job.Context.CorrelationId;

        // Lógica de geração do relatório...
        await Task.CompletedTask;
    }
}
```

```csharp
using FieldService.Queue.Interfaces;
using FieldService.Queue.Types;

public static class GerarRelatorioEndpoint
{
    public static IResult Handle(
        Guid prestadorId,
        IQueueProducer queueProducer,
        IUserContext userContext,
        IRequestContext requestContext)
    {
        var payload = new RelatorioPrestadorPayload(prestadorId);

        var job = Job<RelatorioPrestadorPayload>.Create(
            payload: payload,
            tenantId: userContext.TenantId,
            correlationId: requestContext.CorrelationId);

        // O módulo chama apenas o IQueueProducer (sem acoplamento ao Hangfire)
        var jobId = queueProducer.Publish<RelatorioPrestadorConsumer, RelatorioPrestadorPayload>(job);

        return Results.Accepted(value: new { JobId = jobId });
    }
}
```

Fluxo:
1. Endpoint cria o `payload`.
2. Endpoint cria `Job<TPayload>` com `tenantId` e `correlationId`.
3. Producer publica via `IQueueProducer`.
4. Hangfire executa `QueueJobExecutor`, resolve o consumer no escopo DI e chama `ExecuteAsync(job)`.
5. Consumer processa usando `job.Payload` e `job.Context`.

---

## Modos de envio

### 1. Imediato

```csharp
queueProducer.Publish<TConsumer>(job);
queueProducer.Publish<TConsumer, TRequest>(jobComPayload);
```

### 2. Com atraso

```csharp
queueProducer.PublishDelayed<TConsumer>(job, TimeSpan.FromMinutes(5));
queueProducer.PublishDelayed<TConsumer, TRequest>(jobComPayload, TimeSpan.FromMinutes(5));
```

### 3. Recorrente (CRON)

```csharp
queueProducer.ScheduleRecurring<TConsumer>(job, Cron.Daily);
```

### 4. Remoção de recorrente

```csharp
queueProducer.RemoveRecurring("cleanup-daily");
```

---

## Registro no DI (consumidores)

Registre os consumers do módulo chamando `AddQueueConsumers(...)` com os assemblies que contêm implementações de `IQueueConsumer` e `IQueueConsumer<T>`.

```csharp
services.AddQueueConsumers(typeof(ProcessInvoiceConsumer).Assembly);
```

O módulo Queue resolve os consumers por escopo via `QueueJobExecutor`.

---

## Observabilidade e rastreabilidade

O pipeline Hangfire aplica filtros para padronizar visibilidade:

- **Tags de Dashboard**
  - `Job:{JobType}-{HangfireId}`
  - `Tenant:{TenantId}` ou `Tenant:Multi`
  - `CorrelationId:{CorrelationId}`

- **OpenTelemetry (Activity/Metrics)**
  - `hangfire.job.id`, `hangfire.job.type`, `hangfire.queue`
  - `app.correlation_id`, `app.tenant_id`, `app.trace_id`, `app.is_multi_tenant`
  - métricas de sucesso/falha/duração

Tripla de observabilidade:

```text
TenantId + CorrelationId + TraceId
```

---

## Regras recomendadas para outros módulos

1. **Sempre publicar com `Job`/`Job<TPayload>`** (não usar Hangfire direto).
2. **Sempre preencher `CorrelationId`** no início do fluxo.
3. **Preservar `CorrelationId`** ao encadear novos jobs.
4. **Preencher `TenantId`** quando o job for tenant-bound; usar `null` para rotinas multi-tenant.
5. **Manter consumer com responsabilidade única** (um caso de uso por consumer).
