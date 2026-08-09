using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Interfaces;

public interface IUserAuthenticationRepository
{
    Task Save(UserAuthentication userAuthentication, CancellationToken ct = default);
    Task<UserAuthentication?> GetById(Guid userId,  CancellationToken ct = default);
    Task<UserAuthentication?> GetByExternalId(
        string externalId,
        AuthenticationProvider provider,
        CancellationToken ct = default);
}