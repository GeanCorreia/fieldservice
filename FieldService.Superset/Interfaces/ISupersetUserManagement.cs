using System.ComponentModel.DataAnnotations;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Superset.Dtos.SupersetApiResponseDto;
using Refit;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetUserManagement
{
    /// <summary>
    /// Lista roles do Superset para resolução dos IDs numéricos (ex: Gamma = 3).
    /// </summary>
    [Get("/api/v1/security/roles/")]
    Task<SupersetListApiResponse<SupersetRoleApiResponse>> GetRolesAsync(
        [Url] Uri host,
        [Header("Authorization")] string bearerToken,
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
        [Query("q")] string filterQuery,
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