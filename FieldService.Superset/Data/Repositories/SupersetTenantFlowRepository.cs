using FieldService.Data.Interfaces;
using FieldService.Superset.Data;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Superset.Data.Repositories;

internal sealed class SupersetTenantFlowRepository(
    SupersetDbContext dbContext,
    ISqlUnitOfWork<SupersetDbContext> unitOfWork) : ISupersetTenantFlowRepository
{
    public async Task<SupersetTenantFlow?> GetByIdAsync(
        Guid creationId,
        CancellationToken cancellationToken)
    {
        if (creationId == default)
            return null;

        return await dbContext.SupersetTenantFlows
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == creationId, cancellationToken);
    }

    public async Task<IEnumerable<SupersetTenantFlow>> GetByStatusAsync(
        SupersetTenantDeployStatus status,
        CancellationToken cancellationToken)
    {
        var flows = await dbContext.SupersetTenantFlows
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return flows.Where(x => x.Status == status).ToList();
    }

    public async Task SaveAsync(
        SupersetTenantFlow supersetTenantFlow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(supersetTenantFlow);

        var existing = await dbContext.SupersetTenantFlows
            .FirstOrDefaultAsync(x => x.Id == supersetTenantFlow.Id, cancellationToken);

        if (existing is null)
        {
            await dbContext.SupersetTenantFlows.AddAsync(supersetTenantFlow, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(supersetTenantFlow);
        }

        await unitOfWork.PersistChangesAsync(cancellationToken);
    }
}

