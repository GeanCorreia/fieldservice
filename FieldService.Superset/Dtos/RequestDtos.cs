using System.Text.Json.Serialization;
using FieldService.Shared.Types;

namespace FieldService.Superset.Dtos;

public record SupersetRoleDto(
    int Id,
    string Name);


public record SupersetResourceRequestDto(
    string Type,
    string Id
);

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



