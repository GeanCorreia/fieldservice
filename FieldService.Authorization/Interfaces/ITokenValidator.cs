using System.Security.Claims;

namespace FieldService.Authorization.Interfaces;

public interface ITokenValidator
{
    ClaimsPrincipal ValidateJwt(string token);
    Task<ClaimsPrincipal> ValidateJwtAsync(string token, CancellationToken ct = default);
}
