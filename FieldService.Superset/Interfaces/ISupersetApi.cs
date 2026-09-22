using System.ComponentModel.DataAnnotations;
using Refit;
using FieldService.Superset.Dtos;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetApi
{
    [Get("/health")]
    Task<string> GetHealthAsync(
        [Url] Uri host, 
        CancellationToken cancellationToken = default
    );
    
    [Post("/api/v1/security/login")]
    Task<SupersetLoginResponse> LoginAsync(
        [Url] Uri host,
        [Body] SupersetLoginRequest request, 
        CancellationToken ct = default
    );
    
    [Post("/api/v1/security/guest_token/")]
    Task<SupersetGuestTokenResponse> GetGuestTokenAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken, 
        [Body] SupersetGuestTokenRequest request, 
        CancellationToken ct = default
    );

    [Get("/api/v1/dashboard/")]
    Task<SupersetApiResponse<SupersetDashboardApiItem>> GetDashboardsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        CancellationToken ct = default
    );

    [Get("/api/v1/chart/")]
    Task<SupersetApiResponse<SupersetChartApiItem>> GetChartsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        CancellationToken ct = default
    );

    [Get("/api/v1/dataset/")]
    Task<SupersetApiResponse<SupersetDatasetApiItem>> GetDatasetsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        CancellationToken ct = default
    );

    [Get("/api/v1/saved_query/")]
    Task<SupersetApiResponse<SupersetSavedQueryApiItem>> GetSavedQueriesAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        CancellationToken ct = default
    );
}