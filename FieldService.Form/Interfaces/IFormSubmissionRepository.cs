using FieldService.Form.Entities;
using FieldService.Form.Dtos;
using FieldService.Shared.Types.Http;

namespace FieldService.Form.Interfaces;


internal interface IFormSubmissionRepositoryFactory
{
    Task<IFormSubmissionRepository> CreateAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

internal interface IFormSubmissionRepository : IAsyncDisposable
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
        IEnumerable<FormFieldData> filters,
        PagedRequest? paginationRequestDto = null,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<FormSubmission>> GetAllAsync(
        Guid formId,
        PagedRequest? paginationRequestDto = null,
        CancellationToken cancellationToken = default);
    
    Task<PaginatedResult<FormSubmission>> GetAllDeletedAsync(
        Guid formId,
        PagedRequest? paginationRequestDto = null,
        CancellationToken cancellationToken = default);
}