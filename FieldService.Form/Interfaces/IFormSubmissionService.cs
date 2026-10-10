using System.Text.Json;
using FieldService.Form.Entities;
using FieldService.Shared.Types.Http;

namespace FieldService.Form.Interfaces;

internal interface IFormSubmissionService : IFormSubmissionRepository
{
    Task<FormSubmission> CreateAsync(
        Guid tenantId,
        Guid formId,
        Guid userId,
        JsonElement dataJson,
        CancellationToken cancellationToken = default);

    Task<FormSubmission> UpdateAsync(
        Guid submissionId,
        Guid userId,
        JsonElement dataJson,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid submissionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<FormSubmission>> GetFilteredAsync(
        FormGridFilterRequest request,
        CancellationToken cancellationToken = default);

    Task ReadAsync(
        Guid submissionId,
        Guid userId,
        CancellationToken cancellationToken = default);
}