using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Data.Interfaces;
using FieldService.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Authentication.Data.Repositories;

internal sealed class SessionRepository(
    AuthenticationDbContext dbContext,
    ISqlUnitOfWork<AuthenticationDbContext> unitOfWork) : ISessionRepository
{
    public async Task Save(Session session, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        

        var existing = await dbContext.Sessions
            .FirstOrDefaultAsync(x => x.Id == session.Id, ct);

        if (existing is null)
        {
            await dbContext.Sessions.AddAsync(session, ct);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(session);
        }

        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task Save(IEnumerable<Session> sessions, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var sessionsList = sessions.ToList();
        if (sessionsList.Count == 0)
            return;

        
        var sessionIds = sessionsList.Select(s => s.Id).ToList();
        var existingSessions = await dbContext.Sessions
            .Where(x => sessionIds.Contains(x.Id))
            .ToListAsync(ct);

        // 3. Process each session (add or update)
        foreach (var session in sessionsList)
        {
            var existing = existingSessions.FirstOrDefault(x => x.Id == session.Id);

            if (existing is null)
            {
                await dbContext.Sessions.AddAsync(session, ct);
            }
            else
            {
                dbContext.Entry(existing).CurrentValues.SetValues(session);
            }
        }

        // 4. Persist all changes in a single transaction
        await unitOfWork.PersistChangesAsync(ct);
    }

    public async Task<Session?> GetById(Guid sessionId, CancellationToken ct = default)
    {
        if (sessionId == default)
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        return await dbContext.Sessions
            .FirstOrDefaultAsync(x => x.Id == sessionId, ct);
    }

 

    public async Task<PaginatedResult<Session>> GetInactivitySessions(
        DateTimeOffset? atTime,
        int page = 1,
        int pageSize = 100,
        bool isDescending = false,
        CancellationToken ct = default)
    {
        page = page > 0 ? page : 1;
        pageSize = pageSize > 0 ? pageSize : 100;

        atTime ??= DateTimeOffset.UtcNow;

        var query = dbContext.Sessions
            .Where(sa => sa.RevokedAt == null && sa.ExpiresAt <= atTime.Value);

        var totalCount = await query.LongCountAsync(ct);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        if (totalPages > 0 && page > totalPages)
            page = totalPages;

        var orderedQuery = isDescending
            ? query.OrderByDescending(sa => sa.ExpiresAt)
            : query.OrderBy(sa => sa.ExpiresAt);

        var sessions = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<Session>(
            new Pagination(page, pageSize, totalCount, totalPages),
            sessions);
    }
}
