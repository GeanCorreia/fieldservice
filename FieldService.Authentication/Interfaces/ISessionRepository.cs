using FieldService.Authentication.Entities;
using FieldService.Authentication.Types;

namespace FieldService.Authentication.Interfaces;

public interface ISessionRepository
{
    Task Save(Session session, CancellationToken ct = default);
    Task Save(IEnumerable<Session> sessions, CancellationToken ct = default);
    Task<Session?> GetById(Guid sessionId, CancellationToken ct = default);

}
