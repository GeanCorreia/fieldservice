using FieldService.Superset.Dtos;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Mappers;

namespace FieldService.Superset.Services;

internal class SupersetResourceService : ISupersetResourceService
{
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetService _supersetService;


    public SupersetResourceService(
        ISupersetApi supersetApi, 
        ISupersetService supersetService)
    {
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
    }

    public async Task<SupersetTenantResources> GetTenantResourcesAsync(string fqdnUrl, CancellationToken cancellationToken)
    {
        var hostUri = new Uri(fqdnUrl);
        
        var adminToken = await _supersetService.GetAdminToken(fqdnUrl, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        
        var dashboardsTask = FetchSafelyAsync(() => _supersetApi.GetDashboardsAsync(hostUri, bearerToken, cancellationToken));
        var chartsTask = FetchSafelyAsync(() => _supersetApi.GetChartsAsync(hostUri, bearerToken, cancellationToken));
        var datasetsTask = FetchSafelyAsync(() => _supersetApi.GetDatasetsAsync(hostUri, bearerToken, cancellationToken));
        var queriesTask = FetchSafelyAsync(() => _supersetApi.GetSavedQueriesAsync(hostUri, bearerToken, cancellationToken));
        
        await Task.WhenAll(dashboardsTask, chartsTask, datasetsTask, queriesTask);
        
        var dashboards = (await dashboardsTask)?.Result.Select(d => d.Map()) ?? Enumerable.Empty<SupersetDashboardDto>();
        var charts = (await chartsTask)?.Result.Select(c => c.Map()) ?? Enumerable.Empty<SupersetChartDto>();
        var datasets = (await datasetsTask)?.Result.Select(ds => ds.Map()) ?? Enumerable.Empty<SupersetDatasetDto>();
        var queries = (await queriesTask)?.Result.Select(q => q.Map()) ?? Enumerable.Empty<SupersetSavedQueryDto>();

        return new SupersetTenantResources(dashboards, charts, datasets, queries);
    }
    
    private static async Task<SupersetApiResponse<T>? > FetchSafelyAsync<T>(Func<Task<SupersetApiResponse<T>>> fetchApi)
    {
        try
        {
            return await fetchApi();
        }
        catch
        {
            return null;
        }
    }
}