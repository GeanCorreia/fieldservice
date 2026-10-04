using System.Text.Json.Serialization;

namespace FieldService.Superset.Dtos.SupersetApiResponseDto;


public record SupersetUserResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] int Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("result")] SupersetUserApiResponse Result
);

public record SupersetRoleApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name
);

public record SupersetRolesApiResponse(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("result")] List<SupersetRoleApiResponse> Result
);

public record SupersetRoleDetailApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("permissions")] List<SupersetPermissionResourceApiResponse> Permissions
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



public record SupersetListApiResponse<T>(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("result")] List<T> Result
);

internal record SupersetLoginApiResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken
);

internal record SupersetRefreshTokenApiResponse(
    [property: JsonPropertyName("access_token")] string AccessToken
);

internal record SupersetGuestTokenApiResponse(
    [property: JsonPropertyName("token")] string Token
);


public record SupersetDashboardApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("dashboard_title")] string DashboardTitle,
    [property: JsonPropertyName("published")] bool Published,
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("json_metadata")] string? JsonMetadata, // Contém configurações de filtros nativos e temas
    [property: JsonPropertyName("changed_on")] DateTime? ChangedOn,
    [property: JsonPropertyName("embedded")] List<SupersetEmbeddedConfigApiResponse>? Embedded,
    [property: JsonPropertyName("roles")] List<SupersetRoleApiResponse>? Roles,
    [property: JsonPropertyName("owners")] List<SupersetUserApiResponse>? Owners
);

public record SupersetEmbeddedConfigApiResponse(
    [property: JsonPropertyName("uuid")] string Uuid
);

public record SupersetChartApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("slice_name")] string SliceName,
    [property: JsonPropertyName("viz_type")] string VizType,
    [property: JsonPropertyName("datasource_id")] int? DatasourceId,
    [property: JsonPropertyName("datasource_type")] string? DatasourceType, // "table" ou "query"
    [property: JsonPropertyName("datasource_name_text")] string? DatasourceNameText,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("params")] string? Params, // Parâmetros JSON que definem métricas e dimensões do gráfico
    [property: JsonPropertyName("changed_on")] DateTime? ChangedOn
);

public record SupersetDatasetApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("table_name")] string TableName,
    [property: JsonPropertyName("schema")] string? Schema,
    [property: JsonPropertyName("database_id")] int DatabaseId,
    [property: JsonPropertyName("database_name")] string? DatabaseName,
    [property: JsonPropertyName("kind")] string? Kind, // "physical" (tabela/view real) ou "virtual" (query SQL customizada)
    [property: JsonPropertyName("sql")] string? Sql, // Preenchido se for um Dataset do tipo Virtual (SQL Query)
    [property: JsonPropertyName("filter_select_enabled")] bool FilterSelectEnabled,
    [property: JsonPropertyName("columns")] List<SupersetColumnApiResponse>? Columns,
    [property: JsonPropertyName("metrics")] List<SupersetMetricApiResponse>? Metrics
);

public record SupersetColumnApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("column_name")] string ColumnName,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("filterable")] bool Filterable,
    [property: JsonPropertyName("groupby")] bool Groupby
);

public record SupersetMetricApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("metric_name")] string MetricName,
    [property: JsonPropertyName("expression")] string Expression
);

public record SupersetSavedQueryApiResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("sql")] string Sql,
    [property: JsonPropertyName("db_id")] int DatabaseId,
    [property: JsonPropertyName("schema")] string? Schema,
    [property: JsonPropertyName("created_by")] SupersetUserApiResponse? CreatedBy,
    [property: JsonPropertyName("changed_on")] DateTime? ChangedOn
);