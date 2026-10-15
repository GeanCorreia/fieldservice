using System.Text.Json;
using FieldService.Form.Dtos;
using FieldService.Form.Entities;

namespace FieldService.Form.Interfaces;


internal interface IFormSubmissionService
{
    
    Task CreateAsync(
        Guid tenantId,
        Guid formId,
        Guid userId,
        JsonElement dataJson,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid tenantId,
        Guid submissionId,
        Guid userId,
        JsonElement dataJson,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid tenantId,
        Guid submissionId,
        Guid userId,
        CancellationToken cancellationToken = default);
    
    Task<FormSubmission?> GetByIdAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default);
    
    Task<FormGridDataDto> GetAllDeletedAsync(
        Guid tenantId,
        Guid formId,
        CancellationToken cancellationToken = default);
    
    Task<FormGridDataDto> GetFilteredFormGridData(
        FormGridFilterRequest request,
        CancellationToken cancellationToken);
    
    Task<FormGridDataDto> GetFormGridData(
        FormGridFilterRequest request, 
        CancellationToken cancellationToken);

    
}