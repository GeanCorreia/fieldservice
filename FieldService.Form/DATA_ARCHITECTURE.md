# Form Database Architecture - DbContexts

## 📊 Visão Geral

O módulo Form implementa **dois contextos de banco de dados** para separação de concerns:

```
┌─────────────────────────────────────────┐
│ Application Layer                        │
└──────────────┬──────────────────────────┘
               │
        ┌──────┴───────────┐
        │                  │
        ▼                  ▼
┌──────────────────┐  ┌─────────────────────┐
│ FormDbContext    │  │ FormDataDbContext   │
│ (Core Database)  │  │ (Tenant Database)   │
└──────────────────┘  └─────────────────────┘
        │                  │
        │                  ├─ db_tenant_xxx
        │                  ├─ db_tenant_yyy
        │                  └─ db_tenant_zzz
        │
    form_core_db (single)
```

---

## 🎯 FormDbContext - Core Database

### Propósito
Gerenciar entidades core do Form que são compartilhadas entre todos os tenants.

### Entidades
```csharp
DbSet<Form> Forms               // Definições de formulários
DbSet<FormTenant> FormTenants   // Configurações por tenant
```

### Database
- **Nome:** Determinado pela aplicação (ex: `form_core`, `fieldservice_forms`)
- **Schema:** `form`
- **Instâncias:** 1 única (compartilhada)
- **Isolamento:** Isolamento via TenantId nas queries

### Form Entity (Event Sourced)
```csharp
public class Form
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    
    // Calculated properties (derivadas de events)
    [NotMapped]
    public string Title { get; }      // Last updated title
    
    [NotMapped]
    public string Slug { get; }       // Slugified title
    
    [NotMapped]
    public bool IsActive { get; }     // !deleted
    
    [NotMapped]
    public JsonElement SchemaJson { get; }  // Current schema
    
    // Methods
    public void UpdateForm(Guid userId, string? title, JsonElement? schema);
    public void DeleteForm(Guid userId);
}
```

**Nota:** Form não armazena properties diretamente - tudo é calculado a partir de eventos em `FormEvent`.

### FormTenant Entity
```csharp
public class FormTenant
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    
    // Reference to tenant-specific database
    public Guid ConnectionStringId { get; set; }
    
    // Feature flags
    public bool DeveloperMcpEnabled { get; set; }
    public bool FormExecutionEnabled { get; set; }
}
```

### Configuration
```
Data/Configuration/
├── FormConfiguration.cs         // Maps Form entity
└── FormTenantConfiguration.cs   // Maps FormTenant entity
```

### Usar FormDbContext
```csharp
// Injection
public class MyService(FormDbContext dbContext) { ... }

// Usage
var allFormsForTenant = await dbContext.Forms
    .Where(f => f.TenantId == tenantId)
    .ToListAsync();

var tenantConfig = await dbContext.FormTenants
    .FirstOrDefaultAsync(ft => ft.TenantId == tenantId);
```

---

## 🎯 FormDataDbContext - Tenant-Specific Database

### Propósito
Armazenar dados de submissão de formulários isolados por tenant.

### Entidades
```csharp
DbSet<FormSubmission> FormSubmissions  // Submissões do formulário
```

### Database
- **Padrão:** `db_tenant_{tenantId:N}`
- **Schema:** `form`
- **Instâncias:** N (uma por tenant)
- **Isolamento:** Database-level (diferentes databases)

### FormSubmission Entity
```csharp
public class FormSubmission
{
    public Guid Id { get; set; }
    public Guid FormId { get; set; }
    
    public JsonElement DataJson { get; set; }  // Submission data (jsonb)
    
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeleteByUserId { get; set; }
}
```

### Usar FormDataDbContext

**Via Factory (Recomendado):**
```csharp
// Injection
public class MyService(IFormDataDbContextFactory factory) { ... }

// Usage
var dbContext = await factory.GetFormDataDbContextAsync(tenantId, cancellationToken);
var submissions = await dbContext.FormSubmissions
    .Where(s => s.FormId == formId)
    .ToListAsync();
```

---

## 🔄 Quando Usar Qual Context

| Tarefa | Context | Razão |
|--------|---------|-------|
| Ler/Criar Forms | **FormDbContext** | Core database |
| Ler FormTenant config | **FormDbContext** | Core database |
| Salvar submissões | **FormDataDbContext** | Tenant-specific |
| Ler submissões | **FormDataDbContext** | Tenant-specific |
| Alterar feature flags | **FormDbContext** | Core database |
| Event sourcing | **FormDbContext** (via FormEvent) | Core database |

---

## 🏗️ Arquitetura: Onde Cada Context Vive

### FormDbContext
```
FieldService.Form/Data/FormDbContext.cs

Constructor: FormDbContext(DbContextOptions<FormDbContext> options)
Connection String: From IConfiguration["ConnectionStrings:CoreDatabase"]
```

### FormDataDbContext
```
FieldService.Form/Data/FormDataDbContext.cs

Constructor: FormDataDbContext(DbContextOptions<FormDataDbContext> options)
Connection String: Dinâmico per tenant (via IFormDataDbContextFactory)
```

---

## 🔐 Security & Isolation

### FormDbContext (Core)
- ✅ Usa IConfiguration para connection string
- ✅ Todos podem acessar (com query filters por TenantId)
- ⚠️ Importante: Sempre filtrar por TenantId em queries

```csharp
// ✅ CERTO
var forms = dbContext.Forms.Where(f => f.TenantId == currentTenantId);

// ❌ ERRADO - Expõe forms de outros tenants!
var forms = dbContext.Forms.ToList();
```

### FormDataDbContext (Tenant-Specific)
- ✅ Isolamento garantido via database separado
- ✅ Connection string vem do vault (SecretKey module)
- ✅ Impossível acessar dados de outro tenant (database não existe)

---

## 📊 Relacionamento Entre Contextos

```
FormDbContext (Core DB)
│
├─ Form
│  └─ TenantId ──── referencia tenant
│
└─ FormTenant
   ├─ TenantId
   └─ ConnectionStringId ──┐
                           │
                           └─> [Vault/Secrets]
                               │
                               └─> Connection String
                                   │
                                   └─> FormDataDbContext
                                       │
                                       └─ db_tenant_xxx
                                           │
                                           └─ FormSubmission
```

---

## 🚀 Migrations

### FormDbContext Migrations
```bash
# Criar migration
dotnet ef migrations add [Name] --context FormDbContext

# Gera:
# Data/Migrations/FormDbContext/[timestamp]_[Name].cs
```

### FormDataDbContext Migrations
```bash
# Criar migration
dotnet ef migrations add [Name] --context FormDataDbContext

# Gera:
# Data/Migrations/FormDataDbContext/[timestamp]_[Name].cs
# (Ou current structure se não estiver separado)
```

---

## 💡 Best Practices

### 1. Sempre Filtrar por TenantId
```csharp
// ✅ CERTO
public async Task<Form?> GetFormAsync(Guid formId, Guid tenantId)
{
    return await dbContext.Forms
        .Where(f => f.TenantId == tenantId && f.Id == formId)
        .FirstOrDefaultAsync();
}

// ❌ ERRADO
public async Task<Form?> GetFormAsync(Guid formId)
{
    return await dbContext.Forms
        .Where(f => f.Id == formId)  // Missing TenantId filter!
        .FirstOrDefaultAsync();
}
```

### 2. Use Factory para Tenant Context
```csharp
// ✅ CERTO
var dbContext = await _factory.GetFormDataDbContextAsync(tenantId, cancellationToken);

// ❌ ERRADO - Nunca crie diretamente
var dbContext = new FormDataDbContext(options);  // Qual tenant? Sem segurança!
```

### 3. Encapsule Queries em Repositories
```csharp
public interface IFormRepository
{
    Task<Form?> GetByIdAsync(Guid tenantId, Guid formId);
    Task<IEnumerable<Form>> GetAllAsync(Guid tenantId);
    Task SaveAsync(Form form);
}
```

### 4. Use Using/Using Async para Cleanup
```csharp
await using var dbContext = await _factory.GetFormDataDbContextAsync(tenantId);
try
{
    // Operações
}
catch (Exception ex)
{
    // Handle
}
// dbContext disposto automaticamente
```

---

## 🔗 Related Files

- `PROVISIONING.md` - Processo de provisioning tenant-specific database
- `FormDatabaseService.cs` - Orquestração de provisioning
- `IFormDatabaseService.cs` - Interface de provisioning
- `IFormDataDbContextFactory.cs` - Factory interface

---

**Última atualização:** Outubro 8, 2026
**Versão:** 1.0 - Two-Context Architecture

