using System.ComponentModel.DataAnnotations;
using FieldService.Superset.Dtos;
using Refit;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Superset.Dtos.SupersetApiResponseDto;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetApi
{
    
    [Get("/health")]
    Task<string> GetHealthAsync(
        [Url] Uri host, 
        CancellationToken cancellationToken = default
    );
    
    [Get("/api/v1/security/roles/")]
    Task<SupersetRolesApiResponse> GetRolesAsync(
        [Header("Host")] Uri host, 
        [Header("Authorization")] string bearerToken,
        CancellationToken cancellationToken = default
    );

    [Post("/api/v1/security/roles/")]
    Task<SupersetCreateRoleApiResponse> CreateRoleAsync(
        [Header("Host")] Uri host,
        [Header("Authorization")] string bearerToken,
        [Body] SupersetCreateRoleApiRequest request,
        CancellationToken cancellationToken = default
    );

    [Get("/api/v1/security/permissions-resources/")]
    Task<SupersetListApiResponse<SupersetPermissionResourceApiResponse>> GetPermissionResourcesAsync(
        [Header("Host")] Uri host,
        [Header("Authorization")] string bearerToken,
        [Query("q")] string query,
        CancellationToken cancellationToken = default
    );
    
    [Post("/api/v1/security/login")]
    Task<SupersetLoginApiResponse> LoginAsync(
        [Url] Uri host,
        [Body] SupersetLoginApiRequest request, 
        CancellationToken ct = default
    );
    
    [Post("/api/v1/security/refresh")]
    Task<SupersetRefreshTokenApiResponse> RefreshTokenAsync(
        [Header("Host")] Uri host, 
        [Header("Authorization")] string bearerRefreshToken, 
        CancellationToken cancellationToken = default);
    
    
    [Post("/api/v1/security/guest_token/")]
    Task<SupersetGuestTokenApiResponse> GetGuestTokenAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken, 
        [Body] SupersetGuestTokenApiRequest request, 
        CancellationToken ct = default
    );

    [Get("/api/v1/dashboard/")]
    Task<SupersetListApiResponse<SupersetDashboardApiResponse>> GetDashboardsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        CancellationToken ct = default
    );

    [Get("/api/v1/chart/")]
    Task<SupersetListApiResponse<SupersetChartApiResponse>> GetChartsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        CancellationToken ct = default
    );

    [Get("/api/v1/dataset/")]
    Task<SupersetListApiResponse<SupersetDatasetApiResponse>> GetDatasetsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        CancellationToken ct = default
    );

    [Get("/api/v1/saved_query/")]
    Task<SupersetListApiResponse<SupersetSavedQueryApiResponse>> GetSavedQueriesAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        CancellationToken ct = default
    );
    
    [Get("/api/v1/security/users/")]
    Task<SupersetListApiResponse<SupersetUserApiResponse>> GetTenantSupersetUsersAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        CancellationToken ct = default
    );
}