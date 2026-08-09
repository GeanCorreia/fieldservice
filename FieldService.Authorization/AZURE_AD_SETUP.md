## Integração com Azure AD (Entra ID)

A implementação JWT foi adaptada para utilizar o Azure Active Directory como provedor de tokens.

### Configuração Necessária

#### 1. Registrar a aplicação no Azure AD

1. Acesse [Azure Portal](https://portal.azure.com)
2. Vá para **Azure Active Directory** > **App registrations** > **New registration**
3. Preencha:
   - **Name**: FieldService API
   - **Supported account types**: Selecione conforme sua necessidade
   - **Redirect URI**: deixe em branco por enquanto (é uma API, não web app)
4. Clique em **Register**

#### 2. Obter credenciais

Na página de App registration, anote:
- **Application (Client) ID** → usado como `ClientId`
- Vá para **Overview** e copie o **Directory (Tenant) ID** → usado como `TenantId`

#### 3. Configurar appsettings.Development.json

```json
{
  "AzureAd": {
    "TenantId": "seu-tenant-id-aqui",
    "ClientId": "seu-client-id-aqui",
    "AppIdUri": "api://seu-client-id-aqui",
    "ClockSkewSeconds": 60,
    "IsMultiTenant": false
  }
}
```

#### 4. Registrar o módulo em Program.cs

```csharp
using FieldService.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ... outras configurações ...

builder.Services.AddAuthorizationModule(builder.Configuration);

// ... resto do código ...
```

### Estrutura Implementada

- **`AuthorizationModule.cs`** - Módulo DI que configura autenticação Azure AD
- **`AzureAdTokenValidator.cs`** - Validador de tokens JWT emitidos pelo Azure AD
- **`AzureAdOptions.cs`** - Classe de configuração
- **`JWT.cs`** - Modelo de dados JWT seguindo RFC 7519

### Funcionamento

1. Cliente solicita token ao Azure AD (fora desta aplicação)
2. Cliente inclui o token no header: `Authorization: Bearer {token}`
3. `AzureAdTokenValidator` valida o token usando as chaves públicas do Azure AD
4. Token válido → requisição autorizada
5. Token inválido/expirado → resposta 401 Unauthorized

### Detalhes RFC 7519

O modelo `JWT` implementa todos os campos da RFC 7519:

- **Registered Claims:**
  - `iss` (Issuer) - Emissor do token
  - `sub` (Subject) - Usuário
  - `aud` (Audience) - Destinatários
  - `exp` (Expiration Time) - Expiração
  - `nbf` (Not Before) - Válido a partir de
  - `iat` (Issued At) - Emitido em
  - `jti` (JWT ID) - ID único do token

- **Propriedades adicionais:**
  - `CustomClaims` - Claims customizadas
  - `TokenType` - Tipo de token (Bearer)

### Exemplo de Uso

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MyController : ControllerBase
{
    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        var userId = User.FindFirst("sub")?.Value;
        return Ok(new { userId });
    }
}
```

### Validação de Token Manual

```csharp
[HttpPost("validate")]
public async Task<IActionResult> ValidateToken([FromBody] string token)
{
    try
    {
        var principal = await _tokenValidator.ValidateJwtAsync(token);
        var claims = principal.Claims.Select(c => new { c.Type, c.Value });
        return Ok(new { valid = true, claims });
    }
    catch (Exception ex)
    {
        return BadRequest(new { valid = false, error = ex.Message });
    }
}
```

### Recursos Adicionais

- [Documentação Azure AD](https://learn.microsoft.com/en-us/azure/active-directory/)
- [RFC 7519 - JWT](https://tools.ietf.org/html/rfc7519)
- [Microsoft Identity Platform](https://learn.microsoft.com/en-us/azure/active-directory/develop/)
