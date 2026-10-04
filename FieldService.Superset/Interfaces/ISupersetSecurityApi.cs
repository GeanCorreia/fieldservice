using System.ComponentModel.DataAnnotations;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Superset.Dtos.SupersetApiResponseDto;
using Refit;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetSecurityApi
{
    
    [Get("/api/v1/security/roles/")]
    Task<SupersetRolesApiResponse> GetRolesAsync(
        [Header("Host")] Uri host, 
        [Header("Authorization")] string bearerToken,
        CancellationToken cancellationToken = default
    );

    [Post("/api/v1/security/roles/")]
    Task<SupersetRoleDetailApiResponse> CreateRoleAsync(
        [Header("Host")] Uri host,
        [Header("Authorization")] string bearerToken,
        [Body] SupersetCreateRoleApiRequest request,
        CancellationToken cancellationToken = default
    );
    
    [Get("/api/v1/security/roles/{id}")]
    Task<SupersetRoleDetailApiResponse> GetRoleByIdAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        int id,
        CancellationToken ct = default
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
 
    /// <summary>
    /// Busca usuário por filtro RSQL na API do Superset.
    /// Exemplo do valor em filterQuery: (filters:[(col:username,opr:eq,value:'usr_123')])
    /// </summary>
    [Get("/api/v1/security/users/")]
    Task<SupersetListApiResponse<SupersetUserApiResponse>> GetUsersAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        [Query("q")] string? filterQuery = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Cadastra um novo usuário e vincula suas Roles/Permissões na API do Superset.
    /// </summary>
    [Post("/api/v1/security/users/")]
    Task<SupersetCreateUserApiResponse> CreateUserAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        [Body] SupersetCreateUserApiRequest request,
        CancellationToken ct = default
    );

    /// <summary>
    /// Atualiza os dados e/ou as Roles ativas de um usuário existente.
    /// </summary>
    [Put("/api/v1/security/users/{id}")]
    Task<SupersetUpdateUserApiResponse> UpdateUserAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        int id,
        [Body] SupersetUpdateUserApiRequest request,
        CancellationToken ct = default
    );

    /// <summary>
    /// Remove um usuário da base do Superset pelo ID.
    /// </summary>
    [Delete("/api/v1/security/users/{id}")]
    Task DeleteUserAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
        int id,
        CancellationToken ct = default
    );
}