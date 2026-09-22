namespace FieldService.Superset.Dtos;

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


