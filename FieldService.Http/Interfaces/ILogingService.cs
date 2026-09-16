using FieldService.Authentication.Entities;
using FieldService.Http.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Http.Interfaces;

public interface ILoginService
{
    Task<Guid> LoginAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
    
    Task LogoutAsync(
        RevocationReason reason,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default);
}