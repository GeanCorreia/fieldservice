using System.Text.Json.Serialization;

namespace FieldService.Superset.Dtos.SupersetApiRequestDto;


public record CreateSupersetUserRequest(
    [property: System.Text.Json.Serialization.JsonPropertyName("first_name")] string FirstName,
    [property: System.Text.Json.Serialization.JsonPropertyName("last_name")] string LastName,
    [property: System.Text.Json.Serialization.JsonPropertyName("username")] string Username,
    [property: System.Text.Json.Serialization.JsonPropertyName("email")] string Email,
    [property: System.Text.Json.Serialization.JsonPropertyName("password")] string Password,
    [property: System.Text.Json.Serialization.JsonPropertyName("active")] bool Active,
    [property: System.Text.Json.Serialization.JsonPropertyName("roles")] List<int> RoleIds
);

// Request para ATUALIZAR Usuário/Roles no Superset (PUT /api/v1/security/users/{id})
public record UpdateSupersetUserRequest(
    [property: System.Text.Json.Serialization.JsonPropertyName("first_name")] string FirstName,
    [property: System.Text.Json.Serialization.JsonPropertyName("last_name")] string LastName,
    [property: System.Text.Json.Serialization.JsonPropertyName("email")] string Email,
    [property: System.Text.Json.Serialization.JsonPropertyName("active")] bool Active,
    [property: System.Text.Json.Serialization.JsonPropertyName("roles")] List<int> RoleIds
);

public record SupersetCreateUserApiRequest(
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("active")] bool Active,
    [property: JsonPropertyName("roles")] List<int> RoleIds
);

public record SupersetUpdateUserApiRequest(
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("active")] bool Active,
    [property: JsonPropertyName("roles")] List<int> RoleIds
);

public record SupersetCreateRoleApiRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("permissions")] List<int> PermissionViewMenuIds
);

/// <summary>
/// Enviado no POST /api/v1/security/login
/// </summary>
internal record SupersetLoginApiRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("provider")] string Provider = "db",
    [property: JsonPropertyName("refresh")] bool Refresh = true
);

/// <summary>
/// Enviado no POST /api/v1/security/guest_token/
/// </summary>
internal record SupersetGuestTokenApiRequest(
    [property: JsonPropertyName("user")] SupersetGuestTokenUserApiPayload User,
    [property: JsonPropertyName("resources")] IEnumerable<SupersetResourceApiPayload> Resources,
    [property: JsonPropertyName("rls")] IEnumerable<SupersetRlsApiPayload> Rls
);

internal record SupersetGuestTokenUserApiPayload(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("first_name")] string? FirstName = null,
    [property: JsonPropertyName("last_name")] string? LastName = null
);

internal record SupersetResourceApiPayload(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("id")] string Id
);

internal record SupersetRlsApiPayload(
    [property: JsonPropertyName("clause")] string Clause
);