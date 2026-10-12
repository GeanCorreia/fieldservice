# Form Database Provisioning - Documentação

## 📋 Overview

Este documento descreve o processo de provisioning de infraestrutura de banco de dados para novos tenants no módulo Form. O processo implementa uma adaptação do **OutboxPattern** para garantir idempotência completa e recuperação de falhas.

## 🎯 Objetivo

Garantir que:
- ✅ Cada tenant tenha um banco de dados isolado e seguro
- ✅ O processo seja **idempotente** (pode ser executado múltiplas vezes com segurança)
- ✅ Falhas em qualquer etapa possam ser recuperadas automaticamente
- ✅ Credenciais e connection strings sejam persistidas de forma segura

---

## 🔄 Fluxo de Provisioning

### Arquitetura: OutboxPattern Adaptation + Distributed Lock

```
┌─────────────────────────────────────────────────────────────────┐
│ CreateFormDataInfraAsync (Tenant Provisioning)                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
            ┌─────────────────────────────────────┐
            │ STEP 0: Acquire Distributed Lock    │
            │ ✅ Only one instance can proceed    │
            │ 🔒 Prevents concurrent provisioning │
            └─────────────────────────────────────┘
                              │
                              ▼
            ┌─────────────────────────────────────┐
            │ STEP 1: Ensure Database Exists      │
            │ ✅ CREATE DATABASE IF NOT EXISTS    │
            └─────────────────────────────────────┘
                              │
                              ▼
            ┌─────────────────────────────────────┐
            │ STEP 2: Persist Secret (CHECKPOINT) │
            │ ✅ Save Connection String to Vault  │
            │ 🔒 This is the OutboxPattern Mark   │
            └─────────────────────────────────────┘
                              │
                              ▼
            ┌─────────────────────────────────────┐
            │ STEP 3: Setup Infrastructure        │
            │ ✅ CREATE ROLE (Idempotent)         │
            │ ✅ CREATE SCHEMA IF NOT EXISTS      │
            │ ✅ GRANT Permissions (Idempotent)   │
            └─────────────────────────────────────┘
                              │
                              ▼
            ┌─────────────────────────────────────┐
            │ STEP 4: Apply Migrations            │
            │ ✅ Check Pending Migrations First   │
            │ ✅ MigrateAsync (Idempotent)        │
            └─────────────────────────────────────┘
                              │
                              ▼
            ┌─────────────────────────────────────┐
            │ STEP 5: Release Distributed Lock    │
            │ ✅ Allows other instances to proceed│
            └─────────────────────────────────────┘
                              │
                              ▼
                    Return ConnectionStringId
```

---

## 📊 Detalhes de Cada Step

### Step 1: Ensure Database Exists
```csharp
// Verifica se database existe
if (!await DatabaseExistsAsync(databaseName, ...))
{
    // Cria apenas se não existir
    await ExecuteCommandAsync($"CREATE DATABASE \"{sanitizedDbName}\";", ...);
}
```

**Idempotência:** ✅ Seguro executar múltiplas vezes

---

### Step 2: Persist Secret (CHECKPOINT - CRITICAL)

```csharp
// OutboxPattern: Persiste antes de qualquer operação de banco
if (existingSecret is null)
{
    await PersistConnectionStringSecretAsync(
        connectionStringId,
        tenantId,
        formDatabaseParams.TenantDbConnectionString,
        cancellationToken);
}
```

**Por que é o checkpoint:**
- Uma vez persistido no SecretKey vault, temos uma "marca" que permite recuperação
- Se a aplicação falha APÓS este ponto, a próxima execução encontra o secret e recupera
- Connection string nunca é perdida, mesmo que infraestrutura seja recriada

**Idempotência:** ✅ Se já existe, pula este step

---

### Step 3: Setup Infrastructure

```sql
DO $$
BEGIN
    -- Create or update role (idempotent)
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'form_rw_xxx') THEN
        CREATE ROLE "form_rw_xxx" LOGIN PASSWORD '...' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
    ELSE
        ALTER ROLE "form_rw_xxx" WITH LOGIN PASSWORD '...';
    END IF;
END
$$;

-- Create schema (idempotent)
CREATE SCHEMA IF NOT EXISTS "form";

-- Grant permissions (idempotent - PostgreSQL ignora se já existe)
GRANT CONNECT ON DATABASE "tenant_db_xxx" TO "form_rw_xxx";
GRANT USAGE, CREATE ON SCHEMA "form" TO "form_rw_xxx";
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA "form" TO "form_rw_xxx";
-- ... mais grants ...
```

**Idempotência:** ✅ Todos os comandos usam:
- `IF NOT EXISTS`
- `ALTER` (em vez de CREATE para role)
- PostgreSQL ignora grants duplicados automaticamente

---

### Step 4: Apply Migrations

```csharp
private async Task EnsureFormDataMigrationsAsync(Guid tenantId, CancellationToken cancellationToken)
{
    var dbContext = await _formDataDbContextFactory.GetFormDataDbContextAsync(tenantId, cancellationToken);
    
    // VERIFICA se há pending migrations
    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
    
    if (!pendingMigrations.Any())
    {
        // ✅ Nenhuma pendente? Retorna (otimização)
        return;
    }
    
    // ✅ Se houver, executa (rastreia em __EFMigrationsHistory)
    await dbContext.Database.MigrateAsync(cancellationToken);
}
```

**Idempotência:** ✅ MigrateAsync rastreia aplicação em `__EFMigrationsHistory`

---

## 🔄 Recovery Pattern: EnsureFormDataInfraCheckpointAsync

Quando você precisa validar/recuperar infraestrutura de um tenant:

```csharp
public async Task EnsureFormDataInfraCheckpointAsync(
    Guid tenantId,
    Guid connectionStringId,
    CancellationToken cancellationToken)
{
    // 1. Verifica se secret existe (sem secret = checkpoint não foi alcançado)
    var existingSecret = await TryGetExistingConnectionStringSecretAsync(tenantId, cancellationToken);
    if (existingSecret is null)
        throw new InvalidOperationException("Secret não encontrado - provisioning nunca completou");
    
    // 2. Se schema está faltando, reconstrói infraestrutura
    if (!await SchemaExistsAsync(FormTenant.DatabaseSchema, tenantConnectionStringBuilder, cancellationToken))
    {
        // Reconstrói apenas a infraestrutura (idempotente)
        await EnsureInfrastructureSetupAsync(...);
    }
    
    // 3. Garante migrations
    await EnsureFormDataMigrationsAsync(tenantId, cancellationToken);
}
```

**Uso:** Chame isso se você suspeitar que infraestrutura está incompleta

---

## 🎯 Cenários de Falha & Recuperação

| Cenário | Ocorre Falha? | Próxima Execução | Status |
|---------|---------------|------------------|--------|
| Database creation falha | ❌ Exceção | Retenta criar | ✅ Recuperável |
| Secret persistence falha | ❌ Exceção | **Nenhum checkpoint** - recomeça do zero | ⚠️ Reexecuta tudo |
| Schema/role creation falha | ❌ Exceção | **Checkpoint existe** - reconstrói infra | ✅ Recuperável |
| Migration falha | ❌ Exceção | Retenta migrations | ✅ Recuperável |

**Key Insight:** Uma vez que o secret é persistido (checkpoint), a recuperação é garantida!

---

## 🗄️ Migrations & Entidades

### Current Schema

```
Database: tenant_db_xxx
Schema: form
├── FormSubmission (id, form_id, data_json, deleted_at, delete_by_user_id)
└── __EFMigrationsHistory (rastreia migrations aplicadas)
```

### Entities

- `FormSubmission` - Dados de submissões de formulários
- `Form` - Definição do formulário (em DbContext)
- `FormTenant` - Informações do tenant (em DbContext)

---

## 📝 Guidelines: Alterando Entidades

### ⚠️ **IMPORTANTE: Quando adicionar FormProvising ou modificar qualquer entidade**

Siga este processo:

#### 1️⃣ Adicione/Modifique a Entidade

```csharp
// Exemplo: Adicionar FormProvising
public class FormProvising
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTime ProvisionedAt { get; set; }
    public string Status { get; set; } // pending, completed, failed
}
```

#### 2️⃣ Configure no DbContext

```csharp
// Em FormDataDbContext.OnModelCreating
modelBuilder.Entity<FormProvising>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Status).HasMaxLength(50);
    entity.ToTable("FormProvising", "form");
});
```

#### 3️⃣ Crie Nova Migration

```bash
# Terminal - na pasta FieldService.Form
dotnet ef migrations add AddFormProvising
```

Isso gera:
- `Data/Migrations/[timestamp]_AddFormProvising.cs` - Upates e Downs
- `Data/Migrations/FormDataDbContextModelSnapshot.cs` - **ATUALIZADO AUTOMATICAMENTE**

⚠️ **IMPORTANTE: Hardcoded Entity Names**

O `FormDataDbContextModelSnapshot.cs` é **auto-gerado** e contém strings hardcoded das entidades:

```csharp
// ❌ Ruim: Hardcoded string
modelBuilder.Entity("FieldService.Form.Entities.FormProvising", b => { ... });
```

Quando o EF Core regenera esse arquivo:
1. ✅ Adicione comentário referenciando a classe real:
   ```csharp
   // Maps to FormProvising entity (see: FieldService.Form.Entities.FormProvising)
   modelBuilder.Entity("FieldService.Form.Entities.FormProvising", b => { ... });
   ```

2. ✅ Adicione imports:
   ```csharp
   using FieldService.Form.Entities;  // Para referência direta à classe
   ```

3. ✅ Nunca remova estas anotações - elas ajudam na rastreabilidade se a entidade for renomeada/movida

#### 4️⃣ Review & Commit

```bash
git add Data/Migrations/
git commit -m "feat(form): add FormProvising entity and migration"
```

#### 5️⃣ Deployment

A migration será aplicada automaticamente quando `EnsureFormDataMigrationsAsync` for executada

---

### 🔍 Validando Migrations

```csharp
// Verificar pending migrations em runtime
var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

Console.WriteLine($"Pending: {string.Join(", ", pendingMigrations)}");
Console.WriteLine($"Applied: {string.Join(", ", appliedMigrations)}");
```

---

## 🚀 Processo Completo: Novo Tenant

```csharp
// 1. Application layer
var formService = serviceProvider.GetRequiredService<IFormDatabaseService>();

// 2. Chama provisioning
var connectionStringId = await formService.CreateFormDataInfraAsync(
    tenantId: new Guid("12345678-1234-1234-1234-123456789abc"),
    customHostConnectionStringId: null,
    cancellationToken: default
);

// 3. Resultado:
// ✅ Database criado
// ✅ Secret persistido no vault
// ✅ Role/Schema/Permissions configurados
// ✅ Migrations aplicadas
// ✅ Retorna deterministic connectionStringId

// 4. Store connectionStringId em FormTenant para futura referência
```

---

## 🔐 Segurança

### Credentials
- Usuário: `form_rw_{tenantId:N}` (Read-Write)
- Senha: 32 bytes aleatórios (gerados com `RandomNumberGenerator`)
- Permissions: `NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT`

### Isolation
- Database por tenant (isolamento via network)
- Schema `form` por database
- Role específico por tenant (sem permissões cruzadas)

### Secret Management
- Connection strings persistidas via `FieldService.SecretKey` module
- Vault backend (Azure Key Vault, AWS Secrets, etc.)
- Nunca armazenado em logs

---

## 📚 Arquivos Relevantes

```
FieldService.Form/
├── Services/FormDatabaseService.cs          ← Orquestração do provisioning
├── Entities/
│   ├── Form.cs                              ← Event-sourced Form
│   ├── FormTenant.cs                        ← Tenant config
│   ├── FormSubmission.cs                    ← Submission data
│   └── FormEvent.cs                         ← Events for Form
├── Data/
│   ├── FormDbContext.cs                     ← Core DB Context (Form, FormTenant)
│   ├── FormDataDbContext.cs                 ← Tenant-specific (FormSubmission per tenant)
│   ├── FormDataDbContextFactory.cs          ← Tenant context factory
│   ├── Configuration/
│   │   ├── FormConfiguration.cs             ← Form entity config
│   │   └── FormTenantConfiguration.cs       ← FormTenant entity config
│   └── Migrations/
│       ├── 20261008000000_InitialForm.cs    ← Initial migration (tenant-specific)
│       └── FormDataDbContextModelSnapshot.cs ← Snapshot (auto-generated)
├── Interfaces/
│   ├── IFormDatabaseService.cs              ← Provisioning contract
│   └── IFormDataDbContextFactory.cs         ← Tenant factory contract
└── PROVISIONING.md                          ← Este arquivo
```

### 🎯 Contextos de Banco de Dados

**Dois contextos diferentes:**

#### 1. **FormDbContext** (Core Database)
- **Entidades:** Form, FormTenant
- **Database:** Core Form DB (single instance)
- **Schema:** form
- **Propósito:** Gerenciar definições de formulários e configurações por tenant
- **Acesso:** Compartilhado entre todos os usuários

```csharp
var coreDbContext = serviceProvider.GetRequiredService<FormDbContext>();
var forms = await coreDbContext.Forms.Where(f => f.TenantId == tenantId).ToListAsync();
```

#### 2. **FormDataDbContext** (Tenant-Specific Database)
- **Entidades:** FormSubmission
- **Database:** `db_tenant_{tenantId}` (isolado por tenant)
- **Schema:** form
- **Propósito:** Armazenar submissões de formulários do tenant
- **Acesso:** Isolado via connection string specific

```csharp
var tenantDbContext = await _factory.GetFormDataDbContextAsync(tenantId);
var submissions = await tenantDbContext.FormSubmissions.ToListAsync();
```

---

## 🔗 Related Modules

- **FieldService.SecretKey** - Gerenciamento de secrets (connection strings, credentials)
- **FieldService.Data** - Contextos SQL/Mongo centrais
- **FieldService.Broker** - Publicação de eventos (FormProvisioned, etc.)

---

## ✅ Best Practices: Evitando Hardcoding

### ❌ Anti-Pattern: Strings Hardcoded

```csharp
// Em DbContext.OnModelCreating
modelBuilder.Entity<FormProvising>()  // ✅ Bom - tipo genérico
    .ToTable("FormProvising", "form");

// Mas no snapshot gerado:
modelBuilder.Entity("FieldService.Form.Entities.FormProvising", b => { ... })  // String hardcoded (gerada)
```

Esse hardcoding no snapshot é **inevitável** (gerado pelo EF Core), mas você pode mitigar:

### ✅ Solução: Usar Constantes para Table Names

```csharp
// Entities/FormProvising.cs
public class FormProvising
{
    public const string TableName = "FormProvising";
    public const string SchemaName = "form";
    
    public Guid Id { get; set; }
    // ... properties
}

// Data/FormDataDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<FormProvising>()
        .ToTable(FormProvising.TableName, FormProvising.SchemaName);
}

// Migrations SQL (se necessário manualmente)
CREATE TABLE form.FormProvising (
    Id uuid PRIMARY KEY,
    -- ... columns
);
```

### ✅ Solução: Bem-Documentar o Snapshot

Quando EF Core gera o snapshot, **adicione comentários úteis**:

```csharp
// FormDataDbContextModelSnapshot.cs (após regeneração)
// ⚠️ AUTO-GENERATED by EF Core - DO NOT EDIT MANUALLY

using FieldService.Form.Entities;  // ← Adicione import

// ...

// Maps to FormProvising entity (see: FieldService.Form.Entities.FormProvising)
// Last updated: [timestamp]_AddFormProvising migration
modelBuilder.Entity("FieldService.Form.Entities.FormProvising", b =>
{
    // ... config
});
```

### ✅ Checklist ao Adicionar Entidade

- [ ] Entidade criada em `Entities/`
- [ ] Configuração em `DbContext.OnModelCreating` usando `modelBuilder.Entity<T>()`
- [ ] Migration criada com `dotnet ef migrations add`
- [ ] Snapshot regenerado automaticamente
- [ ] **Após EF Core regenerar:** Adicione comentário "Maps to X entity"
- [ ] Commit migrations junto com entidade

---

## 🔐 Distributed Lock: Preventing Concurrent Provisioning

### Why Lock?

Em ambientes distribuídos (múltiplas instâncias da aplicação), é crítico evitar que dois servidores troquem provisioning do mesmo tenant simultaneamente.

Sem lock:
```
Server 1: CREATE DATABASE tenant_db_xxx ✅
Server 2: CREATE DATABASE tenant_db_xxx ✅ (race condition!)
Server 1: CREATE ROLE ...
Server 2: CREATE ROLE ... (conflict!)
```

Com lock:
```
Server 1: Acquire Lock ✅ → Provision → Release Lock
Server 2: Acquire Lock ⏳ (waiting) → Provision → Release Lock
```

### Lock Implementation

`IFormProcessingLock` fornece sincronização distribuída:

```csharp
public interface IFormProcessingLock
{
    Task<bool> AcquireLock(Guid tenantId, CancellationToken ct);
    Task<bool> ReleaseLock(Guid tenantId, CancellationToken ct);
}
```

### Uso no Provisioning

```csharp
public async Task<Guid> CreateFormDataInfraAsync(
    Guid tenantId,
    Guid? customHostConnectionStringId = null,
    CancellationToken cancellationToken = default)
{
    // ✅ STEP 0: Acquire distributed lock
    if (!await _lock.AcquireLock(tenantId, cancellationToken))
    {
        throw new InvalidOperationException(
            $"Another instance is already provisioning tenant {tenantId}. " +
            $"Please retry after a moment.");
    }
    
    try
    {
        // STEP 1-4: Provisioning steps...
        
        return connectionStringId;
    }
    finally
    {
        // ✅ STEP 5: Always release lock
        await _lock.ReleaseLock(tenantId, cancellationToken);
    }
}
```

### Configuration

```json
{
  "Form": {
    "Lock": {
      "TtlMinutes": 5
    }
  }
}
```

**TtlMinutes:** Tempo máximo que lock é mantido (default: 5 min)
- Deve ser >= tempo máximo do provisioning
- Com falha, lock expira automaticamente

### See Also
Para mais detalhes: `PROCESSING_LOCK.md`

**Q: O que acontece se eu chamar CreateFormDataInfraAsync duas vezes?**
A: A segunda execução é segura (idempotente):
1. Database já existe → skip
2. Secret já existe → skip
3. Infrastructure (role/schema) já existe → PostgreSQL ignora grants duplicados
4. Migrations já aplicadas → rastreia em __EFMigrationsHistory

**Q: E se falhar no meio da criação de role?**
A: Você pode chamar `EnsureFormDataInfraCheckpointAsync` para recuperar. Ele:
1. Verifica se secret existe (se não, erro - nunca foi checkpointado)
2. Se schema está faltando, reconstrói
3. Reaplica migrations se necessário

**Q: Como adiciono uma nova coluna a FormSubmission?**
A: Siga o processo "Guidelines: Alterando Entidades" acima:
1. Modifique `FormSubmission.cs`
2. Configure no `FormDataDbContext.OnModelCreating`
3. `dotnet ef migrations add AddColumnToFormSubmission`
4. Commit migrations
5. On deployment, será aplicada automaticamente

**Q: Posso ter múltiplas schemas no mesmo banco de tenant?**
A: Sim, mas adicione para cada schema:
```sql
CREATE SCHEMA IF NOT EXISTS "form_submissions";
GRANT USAGE, CREATE ON SCHEMA "form_submissions" TO "form_rw_xxx";
```

---

## 📞 Support

Erros comuns:
- `No connection string found` → Secret não foi persistido (checkpoint falhou)
- `ERROR: duplicate key value` → Já existe role/schema (execute 2ª vez com segurança)
- `Migration pending` → Execute `EnsureFormDataMigrationsAsync` novamente

---

**Última atualização:** Outubro 8, 2026
**Versão:** 1.0 - OutboxPattern Adaptation

