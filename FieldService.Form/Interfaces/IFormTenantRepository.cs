using FieldService.Form.Entities;

namespace FieldService.Form.Interfaces;

internal interface IFormTenantRepository
{
    Task<FormTenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FormTenant?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task SaveAsync(FormTenant formTenant, CancellationToken cancellationToken = default);
    
}