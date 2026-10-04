using FieldService.Superset.Dtos;

namespace FieldService.Superset.Interfaces;

public interface ISupersetResourceServices
{
    Task<SupersetTenantResources> GetSupersetTenantResourcesAsync(
        Guid UserId,
        Guid TenantId,
        CancellationToken cancellationToken);
    
    Task<SupersetTenantResources> GetSupersetTenantResourcesAdminAsync(
        Guid TenantId,
        CancellationToken cancellationToken);
    
    Task<IEnumerable<SupersetDashboardDto>> GetDashboardsAsync(
        Guid UserId,
        Guid TenantId,
        CancellationToken cancellationToken);
    
    Task<IEnumerable<SupersetChartDto>> GetChartsAsync(
        Guid UserId,
        Guid TenantId,
        CancellationToken cancellationToken);
    
    Task<IEnumerable<SupersetDatasetDto>> GetDatasetsAsync(
        Guid UserId,
        Guid TenantId,
        CancellationToken cancellationToken);
    
    Task<IEnumerable<SupersetSavedQueryDto>> GetSavedQueriesAsync(
        Guid UserId,
        Guid TenantId,
        CancellationToken cancellationToken);
    
    Task UpdateSupersetTenantResourcesAsync(
        Guid UserId,
        Guid TenantId,
        CancellationToken cancellationToken);
}