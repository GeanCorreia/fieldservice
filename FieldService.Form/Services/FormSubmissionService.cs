using System.Text.Json;
using FieldService.Form.Data.Repositories;
using FieldService.Form.Entities;
using FieldService.Form.Interfaces;
using FieldService.Form.Broker;
using FieldService.Form.Dtos;
using FieldService.Form.Enums;
using FieldService.Shared.Types.Http;

namespace FieldService.Form.Services;

internal sealed class FormSubmissionService(
    FormSubmissionRepository repository,
    IFormService formService,
    BrokerFormSubmissionBrokerProducer brokerProducer) : IFormSubmissionService
{
    private readonly FormSubmissionRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IFormService _formService = formService ?? throw new ArgumentNullException(nameof(formService));
    private readonly BrokerFormSubmissionBrokerProducer _brokerProducer = brokerProducer ?? throw new ArgumentNullException(nameof(brokerProducer));

    public async Task<FormSubmission> CreateAsync(
        Guid tenantId,
        Guid formId,
        Guid userId,
        JsonElement dataJson,
        CancellationToken cancellationToken = default)
    {
        ValidateId(tenantId, nameof(tenantId));
        ValidateId(formId, nameof(formId));
        ValidateId(userId, nameof(userId));

        var form = await EnsureActiveFormAsync(formId, cancellationToken);

        var submission = FormSubmission.CreateRegister(formId, dataJson);
        await _repository.SaveAsync(submission, cancellationToken);

        await PublishCrudEventAsync(tenantId, form.Slug, submission.Id, FormOperation.Create, cancellationToken);

        return submission;
    }

    public async Task<FormSubmission> UpdateAsync(
        Guid submissionId,
        Guid userId,
        JsonElement dataJson,
        CancellationToken cancellationToken = default)
    {
        ValidateId(submissionId, nameof(submissionId));
        ValidateId(userId, nameof(userId));

        var submission = await _repository.GetByIdAsync(submissionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Form submission '{submissionId}' not found.");

        var form = await EnsureActiveFormAsync(submission.FormId, cancellationToken);

        submission.UpdateData(dataJson);
        await _repository.SaveAsync(submission, cancellationToken);

        await PublishCrudEventAsync(form.TenantId, form.Slug, submission.Id, FormOperation.Update, cancellationToken);

        return submission;
    }

    public async Task DeleteAsync(
        Guid submissionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(submissionId, nameof(submissionId));
        ValidateId(userId, nameof(userId));

        var submission = await _repository.GetByIdAsync(submissionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Form submission '{submissionId}' not found.");

        var form = await EnsureActiveFormAsync(submission.FormId, cancellationToken);

        submission.MarkAsDeleted(userId);
        await _repository.SaveAsync(submission, cancellationToken);

        await PublishCrudEventAsync(form.TenantId, form.Slug, submission.Id, FormOperation.Delete, cancellationToken);
    }

    public async Task ReadAsync(
        Guid submissionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(submissionId, nameof(submissionId));
        ValidateId(userId, nameof(userId));

        var submission = await _repository.GetByIdIncludingDeletedAsync(submissionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Form submission '{submissionId}' not found.");

        if (!submission.IsDelete)
            return;

        var form = await EnsureActiveFormAsync(submission.FormId, cancellationToken);

        submission.UndoDeletion();
        await _repository.SaveAsync(submission, cancellationToken);

        // Restore keeps current broker contract by emitting Update.
        await PublishCrudEventAsync(form.TenantId, form.Slug, submission.Id, FormOperation.Update, cancellationToken);
    }

    public async Task<FormSubmission?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id, nameof(id));

        var submission = await _repository.GetByIdAsync(id, cancellationToken);
        if (submission is null)
            return null;

        _ = await EnsureActiveFormAsync(submission.FormId, cancellationToken);
        return submission;
    }

    public async Task<FormSubmission?> GetByIdIncludingDeletedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id, nameof(id));

        var submission = await _repository.GetByIdIncludingDeletedAsync(id, cancellationToken);
        if (submission is null)
            return null;

        _ = await EnsureActiveFormAsync(submission.FormId, cancellationToken);
        return submission;
    }

    public async Task SaveAsync(
        FormSubmission formSubmission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(formSubmission);

        _ = await EnsureActiveFormAsync(formSubmission.FormId, cancellationToken);
        await _repository.SaveAsync(formSubmission, cancellationToken);
    }

    public async Task<PaginatedResult<FormSubmission>> GetFilteredAsync(
        FormGridFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(request.FormId, nameof(request.FormId));

        var filters = NormalizeFilters(request.Filters).ToArray();

        _ = await EnsureActiveFormAsync(request.FormId, cancellationToken);

        if (filters.Length == 0)
        {
            return await _repository.GetAllAsync(
                request.FormId,
                request.PagedRequest,
                cancellationToken);
        }

        return await _repository.GetByFilterAsync(
            request.FormId,
            filters,
            request.PagedRequest,
            cancellationToken);
    }

    public async Task<PaginatedResult<FormSubmission>> GetByFilterAsync(
        Guid formId,
        IEnumerable<FormFieldFilter> filters,
        PagedRequest? paginationRequestDto = null,
        CancellationToken cancellationToken = default)
    {
        ValidateId(formId, nameof(formId));
        ArgumentNullException.ThrowIfNull(filters);

        _ = await EnsureActiveFormAsync(formId, cancellationToken);
        return await _repository.GetByFilterAsync(formId, filters, paginationRequestDto, cancellationToken);
    }

    public async Task<PaginatedResult<FormSubmission>> GetAllAsync(
        Guid formId,
        PagedRequest? paginationRequestDto = null,
        CancellationToken cancellationToken = default)
    {
        ValidateId(formId, nameof(formId));

        _ = await EnsureActiveFormAsync(formId, cancellationToken);
        return await _repository.GetAllAsync(formId, paginationRequestDto, cancellationToken);
    }

    private async Task<FormDto> EnsureActiveFormAsync(Guid formId, CancellationToken cancellationToken)
    {
        var form = await _formService.GetFormDtoAsync(formId, cancellationToken);
        if (form is null)
        {
            throw new KeyNotFoundException($"Form '{formId}' not found.");
        }
        return form;
    }

    private async Task PublishCrudEventAsync(
        Guid tenantId,
        string slugifyFormTitle,
        Guid submissionId,
        FormOperation operation,
        CancellationToken cancellationToken)
    {
        var payload = new BrokerFormSubmissionPayload(
            TenantId: tenantId,
            SlugifyFormTitle: slugifyFormTitle,
            SubmissionId: submissionId,
            Operation: operation);

        await _brokerProducer.PublishAsync(payload, cancellationToken);
    }

    private static IEnumerable<FormFieldFilter> NormalizeFilters(IEnumerable<FormFieldData> filters)
    {
        foreach (var filter in filters ?? [])
        {
            if (string.IsNullOrWhiteSpace(filter.FieldName))
                continue;

            if (filter.Value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
                continue;

            yield return new FormFieldFilter(
                filter.FieldName.Trim(),
                filter.Value!);
        }
    }

    private static void ValidateId(Guid id, string paramName)
    {
        if (id == default)
            throw new ArgumentException("Id cannot be empty.", paramName);
    }
}
