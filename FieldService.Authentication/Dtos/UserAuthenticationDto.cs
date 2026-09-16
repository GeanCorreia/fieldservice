using FieldService.Authentication.Types;

namespace FieldService.Authentication.Dtos;

public record TenantAuthenticationDto(
    Guid TenantId,
    string Name);

public record UserAuthenticationDto(
    Guid UserId,
    IEnumerable<TenantAuthenticationDto> Tenants);