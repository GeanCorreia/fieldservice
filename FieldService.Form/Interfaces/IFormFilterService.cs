using FieldService.Form.Entities;
using FieldService.Form.Dtos;
using FieldService.Shared.Types.Http;

namespace FieldService.Form.Interfaces;

internal interface IFormFilterService
{

    Task<FormGridDataDto> MapToFormGridDataAsync(
        Guid formId,
        Guid tenantId,
        PaginatedResult<FormSubmission> pagedSubmissions,
        CancellationToken cancellationToken);
}