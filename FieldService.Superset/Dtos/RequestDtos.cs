using FieldService.Shared.Types;

namespace FieldService.Superset.Dtos;

public record SupersetRoleDto(
    int Id,
    string Name);

public record SupersetTenantResources(
    IEnumerable<SupersetDashboardDto> Dashboards,
    IEnumerable<SupersetChartDto> Charts,
    IEnumerable<SupersetDatasetDto> Datasets,
    IEnumerable<SupersetSavedQueryDto> SavedQueries)
{
    
    public IEnumerable<SupersetDashboardDto> EmbeddableDashboards => 
        Dashboards.Where(d => d.IsEmbeddable);
}
public record SupersetResourceDto(
    string ResourceId,
    string Name,
    string ResourceType,
    string? Description = null);
    
public record SupersetDashboardDto(
    SupersetResourceDto Resource,
    bool IsPublished,
    string? EmbeddedUuid = null)
{
    public bool IsEmbeddable => IsPublished && !string.IsNullOrWhiteSpace(EmbeddedUuid);
}

public record SupersetChartDto(
    SupersetResourceDto Resource,
    string VizType);
    
public record SupersetDatasetDto(
    SupersetResourceDto Resource,
    string TableName,
    string? Schema = null);
    
public record SupersetSavedQueryDto(
    SupersetResourceDto Resource,
    string Sql,
    string? DatabaseName = null);

/// <summary>
/// Solicitação interna do FieldService para autenticar e gerar um Guest Token
/// </summary>
public record GenerateGuestTokenRequest(
    string Username,
    string DashboardId,
    string? TenantRlsClause = null
);

/// <summary>
/// Solicitação interna para provisionar recursos/dashboards de um Tenant
/// </summary>
public record ProvisionTenantResourcesRequest(
    Guid TenantId,
    string TenantName);

public record SupersetUserDetailDto(
    int SupersetUserId,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    bool Active,
    List<Permission> Permissions
);

public record SupersetUserUpdateRequest(
    string? FirstName = null,
    string? LastName = null,
    string? Email = null,
    List<Permission>? Permissions = null
);

