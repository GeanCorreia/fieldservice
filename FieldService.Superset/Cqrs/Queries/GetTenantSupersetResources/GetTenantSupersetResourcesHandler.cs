using FieldService.Http.Cqrs.Queries.GetUserTenant;
using FieldService.Superset.Dtos;
using FieldService.Superset.Dtos.SupersetApiResponseDto;
using FieldService.Superset.Entities;
using FieldService.Superset.Exceptions;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Jobs;
using FieldService.Superset.Mappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FieldService.Superset.Cqrs.Queries.GetTenantSupersetResources;

internal class GetTenantSupersetResourcesHandler : IRequestHandler<GetTenantSupersetResourcesQuery, SupersetTenantResources>
{
    private readonly ISupersetService _supersetService;
    private readonly ILogger<GetTenantSupersetResourcesHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ISupersetApi _supersetApi;
    private readonly ISupersetAuthService _supersetAuthService;

    public GetTenantSupersetResourcesHandler(
        ISupersetService supersetService, 
        ILogger<GetTenantSupersetResourcesHandler> logger, 
        IMediator mediator,
        ISupersetApi supersetApi,
        ISupersetAuthService supersetAuthService)
    {
        _supersetService = supersetService ?? throw new ArgumentNullException(nameof(supersetService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _supersetApi = supersetApi ?? throw new ArgumentNullException(nameof(supersetApi));
        _supersetAuthService = supersetAuthService ?? throw new ArgumentNullException(nameof(supersetAuthService));
    }

    public async Task<SupersetTenantResources> Handle(
        GetTenantSupersetResourcesQuery request, 
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(
            new GetUserTenantQuery(request.UserId, request.TenantId), 
            cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }
        
        var supersetTenantConfig = await _supersetService
                .GetSupersetTenantByTenantIdAsync(user.TenantDto.TenantId, cancellationToken);

        if (supersetTenantConfig == null)
        {
            throw new SupersetTenantNotFoundException(user.TenantDto.TenantId);
        }
        
        var adminToken = await _supersetAuthService.GetAdminToken(user.TenantDto.TenantId, cancellationToken);
        var bearerToken = $"Bearer {adminToken}";
        
        var fqdnUrl = new Uri(supersetTenantConfig.FqdnUrl);
        
        var dashboardsTask = FetchSafelyAsync(() => _supersetApi
            .GetDashboardsAsync(fqdnUrl, bearerToken, cancellationToken));
        
        var chartsTask = FetchSafelyAsync(() => _supersetApi
            .GetChartsAsync(fqdnUrl, bearerToken, cancellationToken));
        
        var datasetsTask = FetchSafelyAsync(() => _supersetApi
            .GetDatasetsAsync(fqdnUrl, bearerToken, cancellationToken));
        
        var queriesTask = FetchSafelyAsync(() => _supersetApi
            .GetSavedQueriesAsync(fqdnUrl, bearerToken, cancellationToken));
        
        await Task.WhenAll(dashboardsTask, chartsTask, datasetsTask, queriesTask);
        
        var dashboards = (await dashboardsTask)?.Result.Select(d => d.Map()) ?? Enumerable.Empty<SupersetDashboardDto>();
        var charts = (await chartsTask)?.Result.Select(c => c.Map()) ?? Enumerable.Empty<SupersetChartDto>();
        var datasets = (await datasetsTask)?.Result.Select(ds => ds.Map()) ?? Enumerable.Empty<SupersetDatasetDto>();
        var queries = (await queriesTask)?.Result.Select(q => q.Map()) ?? Enumerable.Empty<SupersetSavedQueryDto>();

        return new SupersetTenantResources(dashboards, charts, datasets, queries);

    }
    
    private static async Task<SupersetListApiResponse<T>?> FetchSafelyAsync<T>(Func<Task<SupersetListApiResponse<T>>> fetchApi)
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