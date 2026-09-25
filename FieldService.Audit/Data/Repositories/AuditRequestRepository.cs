using FieldService.Audit.Entities;
using FiledService.Audit.Interfaces;

namespace FiledService.Audit.Data.Repositories;

public class AuditRequestRepository(AuditDbContext dbContext) : IAuditRequestRepository
{
    
    public async Task SaveAsync(IEnumerable<AuditRequest> activities, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(activities);

        var activitiesList = activities.ToList();
        if (activitiesList.Count == 0)
            return;

        await dbContext.SessionActivities.AddRangeAsync(activitiesList, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(AuditRequest activity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        await dbContext.SessionActivities.AddAsync(activity, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    
}