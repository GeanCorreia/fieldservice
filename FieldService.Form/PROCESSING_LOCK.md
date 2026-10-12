# Form Processing Lock - Distributed Synchronization

## 📋 Overview

`IFormProcessingLock` é um serviço de lock distribuído que garante que operações críticas de Form (como provisioning, migrations) não sejam executadas concorrentemente por múltiplas instâncias da aplicação.

```
Instance 1              Instance 2              Instance 3
    │                      │                      │
    ├─ Acquire Lock ◀──────┴──────────────────────┤
    │   (SUCCESS)                                  │
    │                                              │
    ├─ Provision Database                         │
    ├─ Setup Infrastructure                       │
    ├─ Apply Migrations                           │
    │                                              │
    └─ Release Lock                               │
                                                   │
                          ├─ Acquire Lock ◀──────┤
                          │   (SUCCESS)           │
                          ├─ Process...           │
                          └─ Release Lock         │
```

---

## 🔐 Implementação

### Características
- ✅ **Distributed:** Funciona entre múltiplas instâncias
- ✅ **TTL-based:** Lock expira automaticamente (default: 5 min)
- ✅ **Async:** Suporta operações assíncronas
- ✅ **Timeout-aware:** Release com timeout para evitar hang
- ✅ **Flexible:** Suporta diferentes backend (SQL, Redis, Azure)

### Uso Básico

```csharp
public class FormProvisioningService
{
    private readonly IFormProcessingLock _lock;
    
    public FormProvisioningService(IFormProcessingLock lock)
    {
        _lock = lock;
    }
    
    public async Task<Guid> ProvisionTenantAsync(Guid tenantId, CancellationToken ct)
    {
        // Tentar adquirir lock
        if (!await _lock.AcquireLock(tenantId, ct))
        {
            throw new InvalidOperationException(
                $"Another instance is already provisioning tenant {tenantId}");
        }
        
        try
        {
            // Operação crítica protegida
            return await PerformProvisioningAsync(tenantId, ct);
        }
        finally
        {
            // Sempre liberar o lock
            await _lock.ReleaseLock(tenantId, ct);
        }
    }
    
    private async Task<Guid> PerformProvisioningAsync(Guid tenantId, CancellationToken ct)
    {
        // Database creation
        // Infrastructure setup
        // Migrations
        // etc.
    }
}
```

### Com Try-Finally Pattern (Recomendado)

```csharp
public async Task ProcessFormAsync(Guid tenantId, Guid formId, CancellationToken ct)
{
    bool lockAcquired = false;
    
    try
    {
        lockAcquired = await _lock.AcquireLock(tenantId, ct);
        
        if (!lockAcquired)
        {
            throw new InvalidOperationException(
                $"Failed to acquire lock for tenant {tenantId}");
        }
        
        // Critical operation
        await PerformCriticalOperationAsync(formId, ct);
    }
    finally
    {
        if (lockAcquired)
        {
            await _lock.ReleaseLock(tenantId, ct);
        }
    }
}
```

---

## 📦 Dependência de Injeção

### Registro no ServiceCollection

```csharp
// Program.cs ou FormModule.cs
services.AddSingleton<IFormProcessingLock, FormProcessingLock>();
```

### Requisitos
- Medallion.Threading package
- IDistributedLockProvider configurado

Exemplo com Medallion.Threading.DistributedLockProvider:
```csharp
// Program.cs
services.AddSingleton<IDistributedLockProvider>(
    _ => new SqlServerDistributedLockProvider("connection_string"));

services.AddSingleton<IFormProcessingLock, FormProcessingLock>();
```

---

## ⚙️ Configuração

### appsettings.json

```json
{
  "Form": {
    "Lock": {
      "TtlMinutes": 5
    }
  }
}
```

### Default
- **TtlMinutes:** 5 (lock expira em 5 minutos)
- Mínimo: 1 minuto (validado no construtor)

### Quando Ajustar?

| Valor | Caso de Uso |
|-------|-----------|
| 3-5 min | Provisioning rápido, migrations pequenas |
| 10-15 min | Databases grandes, migrations complexas |
| 30 min | Operações muito pesadas |

⚠️ **Cuidado:** Se TTL < tempo da operação, lock pode expirar durante execução!

---

## 🎯 Casos de Uso

### 1. Form Provisioning
```csharp
public async Task<Guid> CreateFormDataInfraAsync(Guid tenantId, ...)
{
    if (!await _lock.AcquireLock(tenantId, cancellationToken))
        throw new Exception("Provisioning already in progress");
    
    try
    {
        // Provision database, infrastructure, migrations
        return await PerformProvisioningAsync(...);
    }
    finally
    {
        await _lock.ReleaseLock(tenantId, cancellationToken);
    }
}
```

### 2. Form Creation/Update
```csharp
public async Task CreateFormAsync(Guid tenantId, CreateFormRequest request, ...)
{
    if (!await _lock.AcquireLock(tenantId, ct))
        throw new InvalidOperationException("Another operation in progress");
    
    try
    {
        // Create form, apply domain events
        var form = Form.Create(tenantId, userId, schema, title);
        await _dbContext.Forms.AddAsync(form);
        await _dbContext.SaveChangesAsync(ct);
    }
    finally
    {
        await _lock.ReleaseLock(tenantId, ct);
    }
}
```

### 3. Batch Processing
```csharp
public async Task ProcessSubmissionsAsync(Guid tenantId, IEnumerable<Guid> submissionIds, ...)
{
    if (!await _lock.AcquireLock(tenantId, ct))
        return false;  // Skip if another process is running
    
    try
    {
        foreach (var submissionId in submissionIds)
        {
            await ProcessSubmissionAsync(submissionId, ct);
        }
        return true;
    }
    finally
    {
        await _lock.ReleaseLock(tenantId, ct);
    }
}
```

---

## 🔍 Comportamento Detalhado

### Acquire Lock
```
AcquireLock(tenantId)
    │
    ├─ Chama TryAcquireAsync(tenantId, ct, storeHandle: true)
    │   │
    │   ├─ Solicita lock ao provider com key: "FormProcessing:{tenantId}"
    │   │   TTL: configurado (default 5 min)
    │   │
    │   ├─ Se conseguir handle:
    │   │   ├─ Armazena em ConcurrentDictionary
    │   │   └─ Retorna true
    │   │
    │   └─ Se handle nulo (lock bloqueado):
    │       └─ Retorna false
    │
    └─ Return: bool
```

### Release Lock
```
ReleaseLock(tenantId)
    │
    ├─ Localiza handle em ConcurrentDictionary
    │   │
    │   ├─ Se não encontrado:
    │   │   └─ Retorna false
    │   │
    │   └─ Se encontrado:
    │       │
    │       ├─ Remove do dicionário
    │       │
    │       ├─ Tenta disposar handle:
    │       │   ├─ Se IAsyncDisposable: await DisposeAsync()
    │       │   └─ Senão: Dispose()
    │       │
    │       ├─ Com timeout de _releaseTimeout (= TTL)
    │       │
    │       └─ Se ObjectDisposedException: retorna false
    │
    └─ Return: bool
```

---

## ⚠️ Best Practices

### ✅ DO

```csharp
// Sempre usar try-finally
bool lockAcquired = await _lock.AcquireLock(tenantId, ct);
try
{
    if (!lockAcquired)
        throw new InvalidOperationException("Couldn't acquire lock");
    
    // Operation
}
finally
{
    if (lockAcquired)
        await _lock.ReleaseLock(tenantId, ct);
}

// Verificar resultado de Acquire
if (!await _lock.AcquireLock(tenantId, ct))
{
    // Handle lock failure
}

// Usar CancellationToken
await _lock.AcquireLock(tenantId, cancellationToken);
```

### ❌ DON'T

```csharp
// Sem verificação de resultado
await _lock.AcquireLock(tenantId, ct);
// ... assume que conseguiu

// Sem try-finally
await _lock.AcquireLock(tenantId, ct);
// ... se exceção, nunca libera!

// Com timeout curto demais
// Configurar TTL < tempo de operação = DEADLOCK

// Sem CancellationToken
await _lock.AcquireLock(tenantId);  // Pode pendurar indefinidamente
```

---

## 🐛 Troubleshooting

| Problema | Causa | Solução |
|----------|-------|--------|
| "Lock could not be acquired" | Outra instância está processando | Aguardar ou aumentar TTL |
| "ObjectDisposedException" | Lock expirou durante execução | Aumentar TtlMinutes em config |
| "Timeout waiting for release" | Release demorou demais | Verificar logs, aumentar _releaseTimeout |
| "Lock never released" | Exceção sem finally | Usar try-finally pattern |

---

## 🔗 Related

- `FormDatabaseService.cs` - Principal consumidor (provisioning)
- `IFormProcessingLock.cs` - Interface
- `FormProcessingLock.cs` - Implementação

---

**Última atualização:** Outubro 8, 2026
**Versão:** 1.0 - Distributed Lock Service

