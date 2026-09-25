using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Interfaces;

public interface ISessionService
{
    Task SaveSessionAsync(
        Session session, 
        CancellationToken ct = default);
    Task SaveSessionsAsync(
        IEnumerable<Session> sessions, 
        CancellationToken ct = default);
    Task<string?> GetSessionJwtIdAsync(
        Guid sessionId, 
        CancellationToken ct = default);
    Task UpdateSessionJwtIdAsync(Guid sessionId, 
        string jwtId, 
        DateTimeOffset expiresAt,
        CancellationToken ct = default);
    Task<Session?> GetSessionAsync(
        Guid sessionId, 
        CancellationToken ct = default);
    Task RemoveSessionAsync(
        Guid sessionId, 
        CancellationToken ct = default);
}