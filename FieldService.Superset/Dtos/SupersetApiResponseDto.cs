using System.Text.Json.Serialization;

namespace FieldService.Superset.Dtos.SupersetApiResponseDto;


public record SupersetUserResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] int Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("result")] SupersetUserResultDto Result
);

public record SupersetRoleApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name
);

public record SupersetRolesApiResponse(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("result")] List<SupersetRoleApiResponse> Result
);

public record SupersetCreateRoleApiResponse(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("result")] SupersetRoleApiResponse? Result
);

public record SupersetNamedEntityApiResponse(
    [property: JsonPropertyName("name")] string? Name
);

public record SupersetPermissionResourceApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("permission_name")] string? PermissionName,
    [property: JsonPropertyName("view_menu_name")] string? ViewMenuName,
    [property: JsonPropertyName("permission")] SupersetNamedEntityApiResponse? Permission,
    [property: JsonPropertyName("view_menu")] SupersetNamedEntityApiResponse? ViewMenu
)
{
    public string? EffectivePermissionName => PermissionName ?? Permission?.Name;
    public string? EffectiveViewMenuName => ViewMenuName ?? ViewMenu?.Name;
}



public record SupersetUserApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("active")] bool Active,
    [property: JsonPropertyName("roles")] List<SupersetRoleApiResponse>? Roles
);

public record SupersetCreateUserApiResponse(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("result")] SupersetUserApiResponse? Result
);

public record SupersetUpdateUserApiResponse(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("result")] SupersetUserApiResponse? Result
);

public record SupersetUserResultDto(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] int Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("username")] string Username,
    [property: System.Text.Json.Serialization.JsonPropertyName("email")] string Email,
    [property: System.Text.Json.Serialization.JsonPropertyName("first_name")] string FirstName,
    [property: System.Text.Json.Serialization.JsonPropertyName("last_name")] string LastName,
    [property: System.Text.Json.Serialization.JsonPropertyName("active")] bool Active
);

/// <summary>
/// Wrapper genérico de listagens da API do Superset GET /api/v1/{resource}/
/// </summary>
public record SupersetListApiResponse<T>(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("result")] List<T> Result
);

/// <summary>
/// Resposta do POST /api/v1/security/login
/// </summary>
internal record SupersetLoginApiResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken
);

internal record SupersetRefreshTokenApiResponse(
    [property: JsonPropertyName("access_token")] string AccessToken
);

/// <summary>
/// Resposta do POST /api/v1/security/guest_token/
/// </summary>
internal record SupersetGuestTokenApiResponse(
    [property: JsonPropertyName("token")] string Token
);

// --- Recursos do Superset (Respostas) ---

public record SupersetDashboardApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("dashboard_title")] string DashboardTitle,
    [property: JsonPropertyName("published")] bool Published,
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("embedded")] List<SupersetEmbeddedConfigApiResponse>? Embedded
);

public record SupersetEmbeddedConfigApiResponse(
    [property: JsonPropertyName("uuid")] string Uuid
);

public record SupersetChartApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("slice_name")] string SliceName,
    [property: JsonPropertyName("viz_type")] string VizType
);

public record SupersetDatasetApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("table_name")] string TableName,
    [property: JsonPropertyName("schema")] string? Schema
);

public record SupersetSavedQueryApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("sql")] string Sql
);