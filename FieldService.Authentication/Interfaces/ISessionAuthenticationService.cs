using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Interfaces;

public interface ISessionAuthenticationService
{
     Task<Session?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}