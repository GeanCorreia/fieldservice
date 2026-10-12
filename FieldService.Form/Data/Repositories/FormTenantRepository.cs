using FieldService.Data.Interfaces;
using FieldService.Form.Entities;
using FieldService.Form.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Form.Data.Repositories;

internal sealed class FormTenantRepository(
    FormDbContext dbContext,
    ISqlUnitOfWork<FormDbContext> unitOfWork) : IFormTenantRepository
{
    public async Task<FormTenant?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id, nameof(id));

        return await dbContext.FormTenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<FormTenant?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(tenantId, nameof(tenantId));

        return await dbContext.FormTenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
    }

    public async Task SaveAsync(
        FormTenant formTenant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(formTenant);

        var existing = await dbContext.FormTenants
            .FirstOrDefaultAsync(x => x.Id == formTenant.Id, cancellationToken);

        if (existing is null)
        {
            await dbContext.FormTenants.AddAsync(formTenant, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(formTenant);
        }

        await unitOfWork.PersistChangesAsync(cancellationToken);
    }

    private static void ValidateId(Guid id, string paramName)
    {
        if (id == default)
            throw new ArgumentException("Id cannot be empty.", paramName);
    }
}

