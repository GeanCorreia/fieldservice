using FieldService.Form.Dtos;

namespace FieldService.Form.Interfaces;



internal interface IFormService 
{
    Task<IEnumerable<FormInfoDto>> GetFormInfosByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
    
    Task<FormDto?> GetFormDtoAsync(
        Guid formId, 
        Guid tenantId,
        CancellationToken cancellationToken);
    
    Task<IEnumerable<FormDto>> GetFormsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
    
    Task CreateFormAsync(
        FormDto form,
        Guid userId,
        CancellationToken cancellationToken);
    
    Task UpdateFormAsync(
        FormDto form,
        Guid userId,
        CancellationToken cancellationToken);
    
    Task DeleteFormAsync(
        Guid formId,
        Guid userId,
        CancellationToken cancellationToken);
}