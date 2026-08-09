# Implementando IResourceDefinition nos Módulos

Cada módulo que deseja usar o sistema de permissões da aplicação deve implementar a interface `IResourceDefinition` para definir seus recursos e ações permitidas.

## O que é IResourceDefinition?

`IResourceDefinition` é uma interface que define quais recursos e ações um módulo disponibiliza para ser protegido por permissões.

```csharp
public interface IResourceDefinition
{
    /// <summary>
    /// Nome único do recurso (ex: "users", "projects", "reports")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Ações permitidas para este recurso (ex: "read", "create", "delete")
    /// </summary>
    IEnumerable<string> GetActions();
}
```

## Como Implementar

### 1. Criar a classe de definição de recurso

Crie uma classe em seu módulo que implemente `IResourceDefinition`:

```csharp
// FieldService.Data/Resources/UserResourceDefinition.cs
using FieldService.Shared.Types;

namespace FieldService.Data.Resources;

public class UserResourceDefinition : IResourceDefinition
{
    public string Name => "users";

    public IEnumerable<string> GetActions() =>
        new[]
        {
            "read",      // Listar/Visualizar usuários
            "create",    // Criar novo usuário
            "update",    // Editar usuário
            "delete",    // Deletar usuário
            "approve"    // Aprovar usuário
        };
}
```

### 2. Registrar no seu módulo

No seu `Module.cs`, registre a definição de recurso:

```csharp
// FieldService.Data/DataModule.cs
using FieldService.Data.Resources;

namespace FieldService.Data;

public static class DataModule
{
    public static IServiceCollection AddDataModule(this IServiceCollection services)
    {
        // ... suas configurações de dados ...

        // Registre seus recursos
        ResourceRegistry.Register(new UserResourceDefinition());
        ResourceRegistry.Register(new ProjectResourceDefinition());
        
        return services;
    }
}
```

### 3. Auto-descoberta (Recomendado)

OU deixe o sistema auto-descobrir automaticamente em `Program.cs`:

```csharp
// Program.cs
using FieldService.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Registra autenticação
builder.Services.AddAuthorizationModule(builder.Configuration);

// Auto-descobre e registra todos os IResourceDefinition dos módulos
AuthorizationModule.AutoRegisterResources(
    typeof(DataModule).Assembly,
    typeof(NotificationModule).Assembly,
    typeof(CacheModule).Assembly
    // Adicione aqui todos os assemblies dos seus módulos
);

var app = builder.Build();
// ... resto do código ...
```

## Exemplo Completo

### Módulo de Dados (Data)

```csharp
// FieldService.Data/Resources/UserResourceDefinition.cs
using FieldService.Shared.Types;

namespace FieldService.Data.Resources;

public class UserResourceDefinition : IResourceDefinition
{
    public string Name => "users";

    public IEnumerable<string> GetActions() =>
        new[] { "read", "create", "update", "delete", "approve" };
}

// FieldService.Data/Resources/ProjectResourceDefinition.cs
public class ProjectResourceDefinition : IResourceDefinition
{
    public string Name => "projects";

    public IEnumerable<string> GetActions() =>
        new[] { "read", "create", "update", "delete", "share" };
}
```

### Módulo de Notificações (Notification)

```csharp
// FieldService.Notification/Resources/NotificationResourceDefinition.cs
using FieldService.Shared.Types;

namespace FieldService.Notification.Resources;

public class NotificationResourceDefinition : IResourceDefinition
{
    public string Name => "notifications";

    public IEnumerable<string> GetActions() =>
        new[] { "read", "send", "delete" };
}
```

## Usando no seu Endpoint

Após implementar `IResourceDefinition`, você pode proteger seus endpoints:

```csharp
using FieldService.Authorization.Attributes;

[ApiController]
[Route("api/tenants/{tenantId}/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    [RequirePermission("users", "read")]
    [HttpGet]
    public IActionResult GetUsers(string tenantId)
    {
        return Ok(/* dados */);
    }

    [RequirePermission("users", "create")]
    [HttpPost]
    public IActionResult CreateUser(string tenantId, [FromBody] CreateUserDto dto)
    {
        return Created(/* dados */);
    }

    [RequirePermission("users", "update")]
    [HttpPut("{id}")]
    public IActionResult UpdateUser(string tenantId, string id, [FromBody] UpdateUserDto dto)
    {
        return Ok(/* dados */);
    }

    [RequirePermission("users", "delete")]
    [HttpDelete("{id}")]
    public IActionResult DeleteUser(string tenantId, string id)
    {
        return NoContent();
    }
}
```

## Validação Automática

O sistema valida automaticamente:

1. **Se o recurso existe** - Verifica no `ResourceRegistry`
2. **Se a ação é válida** - Compara com `GetActions()`
3. **Se o usuário tem permissão** - Middleware valida contra banco de dados

### Exemplo de erro:

```csharp
// ❌ ERRO - "users" não existe
[RequirePermission("user-manager", "read")]

// ✓ CORRETO - "users" foi definido em UserResourceDefinition
[RequirePermission("users", "read")]

// ❌ ERRO - "delete" não existe em notifications
[RequirePermission("notifications", "delete")]

// ✓ CORRETO - "send" existe em NotificationResourceDefinition
[RequirePermission("notifications", "send")]
```

## Boas Práticas

1. **Nomes consistentes:**
   - Use nomes em **minúsculas e plural**: `users`, `projects`, `reports`
   - Use nomes descritivos e únicos

2. **Ações semânticas:**
   - `read` - Visualizar/Listar
   - `create` - Criar novo
   - `update` - Editar existente
   - `delete` - Remover
   - `approve` - Aprovar (se aplicável)
   - `export` - Exportar (se aplicável)

3. **Uma classe por recurso:**
   ```csharp
   UserResourceDefinition       // 1 classe
   ProjectResourceDefinition    // 1 classe
   // NÃO combine em uma só
   ```

4. **Organização de pastas:**
   ```
   FieldService.Data/
   ├── Resources/
   │   ├── UserResourceDefinition.cs
   │   └── ProjectResourceDefinition.cs
   └── DataModule.cs
   ```

## Testando seu Recurso

```csharp
// Verificar se recurso foi registrado
var users = ResourceRegistry.Get("users");
Assert.NotNull(users);
Assert.Contains("read", users.GetActions());
Assert.Contains("create", users.GetActions());

// Validar permissão
bool isValid = ResourceRegistry.IsValid("users", "read");
Assert.True(isValid);

// Ação inválida
bool isInvalid = ResourceRegistry.IsValid("users", "invalid-action");
Assert.False(isInvalid);
```

## Fluxo Completo

```
┌─────────────────────────────────┐
│ 1. Módulo cria classe            │
│    UserResourceDefinition        │
│    { Name: "users", ... }        │
└─────────────────────────────────┘
                ↓
┌─────────────────────────────────┐
│ 2. Program.cs auto-registra      │
│    ResourceRegistry.AutoRegister │
└─────────────────────────────────┘
                ↓
┌─────────────────────────────────┐
│ 3. Controller usa @RequirePermission
│    [RequirePermission("users", "read")]
└─────────────────────────────────┘
                ↓
┌─────────────────────────────────┐
│ 4. Middleware valida             │
│    Permission existe no registry? │
│    User tem essa permission?     │
└─────────────────────────────────┘
                ↓
✓ Acesso autorizado ou ✗ Forbidden
```

## Sumário

| O que | Como | Onde |
|------|------|------|
| **Definir recurso** | Criar classe : IResourceDefinition | Seu módulo |
| **Registrar recurso** | ResourceRegistry.Register() ou auto-discovery | Program.cs |
| **Usar em endpoint** | @RequirePermission(resource, action) | Controller |
| **Validação** | Middleware faz automaticamente | Pipeline HTTP |

Pronto! Seu módulo agora faz parte do sistema de permissões! 🎯
