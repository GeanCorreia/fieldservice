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
    
    
    [Get("/api/v1/dashboard/")]
    Task<SupersetListApiResponse<SupersetDashboardApiResponse>> GetDashboardsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        [Query] string? q = null,
        CancellationToken ct = default
    );

    [Get("/api/v1/chart/")]
    Task<SupersetListApiResponse<SupersetChartApiResponse>> GetChartsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        [Query] string? q = null,
        CancellationToken ct = default
    );

    [Get("/api/v1/dataset/")]
    Task<SupersetListApiResponse<SupersetDatasetApiResponse>> GetDatasetsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        [Query] string? q = null,
        CancellationToken ct = default
    );

    [Get("/api/v1/saved_query/")]
    Task<SupersetListApiResponse<SupersetSavedQueryApiResponse>> GetSavedQueriesAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        [Query] string? q = null,
        CancellationToken ct = default
    );
    
    [Get("/api/v1/dashboard/export/")]
    Task<HttpResponseMessage> ExportDashboardsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        [Query] string q,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/chart/export/")]
    Task<HttpResponseMessage> ExportChartsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        [Query] string q,
        CancellationToken cancellationToken = default);
    
    [Get("/api/v1/saved_query/export/")]
    Task<HttpResponseMessage> ExportSavedQueriesAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        [Query] string q,
        CancellationToken cancellationToken = default);
    
    [Multipart]
    [Post("/api/v1/assets/import/")]
    Task ImportAssetsAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        [AliasAs("formData")] StreamPart file,
        [AliasAs("overwrite")] bool overwrite = true,
        CancellationToken cancellationToken = default
    );
    
   
}