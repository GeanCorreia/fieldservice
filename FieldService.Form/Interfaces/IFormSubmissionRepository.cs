using FieldService.Form.Entities;
using FieldService.Shared.Types.Http;

namespace FieldService.Form.Interfaces;

public record FormFieldFilter(
    string FieldName,
    object FieldValue);

internal interface IFormSubmissionRepository
{
    Task<FormSubmission?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FormSubmission?> GetByIdIncludingDeletedAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        FormSubmission formSubmission,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<FormSubmission>> GetByFilterAsync(
        Guid formId,
        IEnumerable<FormFieldFilter> filters,
        PagedRequest? paginationRequestDto = null,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<FormSubmission>> GetAllAsync(
        Guid formId,
        PagedRequest? paginationRequestDto = null,
        CancellationToken cancellationToken = default);
}