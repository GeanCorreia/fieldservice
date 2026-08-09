using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Data.Interfaces;
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
            .Include(x => x.Activities)
            .FirstOrDefaultAsync(x => x.Id == session.Id, ct);

        if (existing is null)
        {
            await dbContext.Sessions.AddAsync(session, ct);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(session);
            dbContext.SessionActivities.RemoveRange(existing.Activities);
            await dbContext.SessionActivities.AddRangeAsync(session.Activities, ct);
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
            .Include(x => x.Activities)
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

                dbContext.SessionActivities.RemoveRange(existing.Activities);
                await dbContext.SessionActivities.AddRangeAsync(session.Activities, ct);
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
            .Include(x => x.Activities)
            .FirstOrDefaultAsync(x => x.Id == sessionId, ct);
    }

    
}
