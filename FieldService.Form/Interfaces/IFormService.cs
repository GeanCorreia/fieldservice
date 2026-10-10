using FieldService.Form.Dtos;

namespace FieldService.Form.Interfaces;

public record FormInfo(
    Guid FormId,
    string Title,
    string? Description
);


internal interface IFormService 
{
    Task<IEnumerable<FormInfo>> GetFormInfosByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
    
    Task<FormDto?> GetFormDtoAsync(
        Guid formId, 
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