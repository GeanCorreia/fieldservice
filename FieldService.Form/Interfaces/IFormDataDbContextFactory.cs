using Microsoft.EntityFrameworkCore;

namespace FieldService.Form.Interfaces;

internal interface IFormDataDbContextFactory
{
    Task<DbContext> GetFormDataDbContextAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

