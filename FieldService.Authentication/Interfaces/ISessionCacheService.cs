using FieldService.Authentication.Types;

namespace FieldService.Authentication.Interfaces;

public interface ISessionCacheService
{
    Task SaveSessionAsync(
        SessionCacheModel session, 
        CancellationToken ct = default);
    Task TouchSessionAsync(
        Guid sessionId,
        SessionActivityCacheModel activity,
        CancellationToken ct = default);
    Task<string?> GetSessionJwtIdAsync(
        Guid sessionId, 
        CancellationToken ct = default);
    Task UpdateSessionJwtIdAsync(Guid sessionId, 
        string jwtId, 
        DateTime expiresAt,
        CancellationToken ct = default);
    Task<SessionCacheModel?> GetSessionAsync(
        Guid sessionId, 
        CancellationToken ct = default);
    Task RemoveSessionAsync(
        Guid sessionId, 
        CancellationToken ct = default);
    Task<bool> ExistsAsync(
        Guid sessionId, 
        CancellationToken ct = default);
    Task<IEnumerable<SessionCacheModel>> GetInactiveCandidatesAsync(
        TimeSpan inactivityThreshold, 
        CancellationToken ct = default);
}