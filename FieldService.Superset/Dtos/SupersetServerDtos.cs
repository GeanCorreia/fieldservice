using System.Text.Json.Serialization;

namespace FieldService.Superset.Dtos;


internal record SupersetLoginRequest(
    string username, 
    string password, 
    string provider = "db", 
    bool refresh = true
);

internal record SupersetLoginResponse(
    string access_token
);


internal record SupersetResourcePayload(
    string type, 
    string id
);

internal record SupersetRlsPayload(
    string clause
);

internal record SupersetGuestTokenRequest(
    string userName,
    IEnumerable<SupersetResourcePayload> resources,
    IEnumerable<SupersetRlsPayload> rls
);

internal record SupersetGuestTokenResponse(
    string token
);

public record SupersetApiResponse<T>(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("result")] List<T> Result
);

// Resource 1: Dashboard
public record SupersetDashboardApiItem(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("dashboard_title")] string DashboardTitle,
    [property: JsonPropertyName("published")] bool Published,
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("embedded")] List<SupersetEmbeddedApiConfig>? Embedded
);

// Resource 2: Chart
public record SupersetChartApiItem(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("slice_name")] string SliceName,
    [property: JsonPropertyName("viz_type")] string VizType
);

// Resource 3: Dataset
public record SupersetDatasetApiItem(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("table_name")] string TableName,
    [property: JsonPropertyName("schema")] string? Schema
);

// Resource 4: Saved Query (SQL)
public record SupersetSavedQueryApiItem(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("sql")] string Sql
);

public record SupersetEmbeddedApiConfig([property: JsonPropertyName("uuid")] string Uuid);