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
    public async Task<SupersetContainerDeploymentFlow?> GetByIdAsync(
        Guid creationId,
        CancellationToken cancellationToken)
    {
        if (creationId == default)
            return null;

        return await dbContext.SupersetTenantFlows
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == creationId, cancellationToken);
    }

    public async Task<IEnumerable<SupersetContainerDeploymentFlow>> GetByStatusAsync(
        SupersetTenantDeployStatus status,
        CancellationToken cancellationToken)
    {
        var flows = await dbContext.SupersetTenantFlows
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return flows.Where(x => x.Status == status).ToList();
    }

    public async Task SaveAsync(
        SupersetContainerDeploymentFlow supersetContainerDeploymentFlow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(supersetContainerDeploymentFlow);

        var existing = await dbContext.SupersetTenantFlows
            .FirstOrDefaultAsync(x => x.Id == supersetContainerDeploymentFlow.Id, cancellationToken);

        if (existing is null)
        {
            await dbContext.SupersetTenantFlows.AddAsync(supersetContainerDeploymentFlow, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(supersetContainerDeploymentFlow);
        }

        await unitOfWork.PersistChangesAsync(cancellationToken);
    }
}

