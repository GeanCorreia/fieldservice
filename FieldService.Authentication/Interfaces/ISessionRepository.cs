using FieldService.Authentication.Entities;
using FieldService.Shared.Responses;

namespace FieldService.Authentication.Interfaces;

public interface ISessionRepository
{
    Task Save(Session session, CancellationToken ct = default);
    Task Save(IEnumerable<Session> sessions, CancellationToken ct = default);
    Task<Session?> GetById(Guid sessionId, CancellationToken ct = default);
    Task<PaginatedResult<Session>> GetInactivitySessions(
        DateTimeOffset? atTime,
        int page = 1,
        int pageSize = 100,
        bool isDescending = false,
        CancellationToken ct = default);

}
