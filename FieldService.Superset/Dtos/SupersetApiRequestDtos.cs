using System.Text.Json.Serialization;

namespace FieldService.Superset.Dtos.SupersetApiRequestDto;



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

internal record SupersetLoginApiRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("provider")] string Provider = "db",
    [property: JsonPropertyName("refresh")] bool Refresh = true
);

internal record SupersetGuestTokenApiRequest(
    [property: JsonPropertyName("user")] SupersetGuestTokenUserApiPayload User,
    [property: JsonPropertyName("resources")] IEnumerable<SupersetResourceApiPayload> Resources,
    [property: JsonPropertyName("rls")] IEnumerable<SupersetRlsApiPayload> Rls
);

internal record SupersetGuestTokenUserApiPayload(
    [property: JsonPropertyName("username")] string Username,
    
    [property: JsonPropertyName("first_name")] 
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] 
    string? FirstName = null,
    
    [property: JsonPropertyName("last_name")] 
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] 
    string? LastName = null
);

internal record SupersetResourceApiPayload(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("id")] string Id
);

internal record SupersetRlsApiPayload(
    [property: JsonPropertyName("clause")] string Clause
){
    public static SupersetRlsApiPayload ForTenant(Guid tenantId) 
        => new($"TenantId = '{tenantId}'");
};


public record SupersetFilter
{
    [JsonPropertyName("filters")]
    public List<SupersetFilterCondition> Filters { get; init; } = new();

    public SupersetFilter() { }

    public SupersetFilter(string column, string operatorCode, object value)
    {
        Filters.Add(new SupersetFilterCondition(column, operatorCode, value));
    }

    public SupersetFilter AddCondition(string column, string operatorCode, object value)
    {
        Filters.Add(new SupersetFilterCondition(column, operatorCode, value));
        return this;
    }
    public override string ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
    
    public static implicit operator string(SupersetFilter filter) => filter.ToString();
}

public record SupersetFilterCondition(
    [property: JsonPropertyName("col")] string Column,
    [property: JsonPropertyName("opr")] string Operator,
    [property: JsonPropertyName("value")] object Value
);