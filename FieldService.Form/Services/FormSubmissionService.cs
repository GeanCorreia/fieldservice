using System.Text.Json;
using FieldService.Form.Entities;
using FieldService.Form.Interfaces;
using FieldService.Form.Broker;
using FieldService.Form.Dtos;
using FieldService.Form.Enums;
using FieldService.Shared.Types.Http;

namespace FieldService.Form.Services;

internal sealed class FormSubmissionService(
    IFormSubmissionRepositoryFactory repositoryFactory,
    IFormService formService,
    BrokerFormSubmissionBrokerProducer brokerProducer,
    IFormFilterService formFilterService) : IFormSubmissionService
{
    private readonly IFormSubmissionRepositoryFactory _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
    private readonly IFormService _formService = formService ?? throw new ArgumentNullException(nameof(formService));
    private readonly BrokerFormSubmissionBrokerProducer _brokerProducer = brokerProducer ?? throw new ArgumentNullException(nameof(brokerProducer));
    private readonly IFormFilterService _formFilterService = formFilterService ?? throw new ArgumentNullException(nameof(formFilterService));

    public async Task CreateAsync(
        Guid tenantId,
        Guid formId,
        Guid userId,
        JsonElement dataJson,
        CancellationToken cancellationToken = default)
    {
        ValidateId(tenantId, nameof(tenantId));
        ValidateId(formId, nameof(formId));
        ValidateId(userId, nameof(userId));

        await using var repository = await _repositoryFactory.CreateAsync(tenantId, cancellationToken);
        var form = await EnsureActiveFormAsync(tenantId, formId, cancellationToken);

        var submission = FormSubmission.CreateRegister(formId, dataJson);
        await repository.SaveAsync(submission, cancellationToken);

        await PublishCrudEventAsync(tenantId, form.Slug, submission.Id, FormOperation.Create, cancellationToken);
    }

    public async Task UpdateAsync(
        Guid tenantId,
        Guid submissionId,
        Guid userId,
        JsonElement dataJson,
        CancellationToken cancellationToken = default)
    {
        ValidateId(tenantId, nameof(tenantId));
        ValidateId(submissionId, nameof(submissionId));
        ValidateId(userId, nameof(userId));

        await using var repository = await _repositoryFactory.CreateAsync(tenantId, cancellationToken);
        
        var submission = await repository.GetByIdAsync(submissionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Form submission '{submissionId}' not found.");

        var form = await EnsureActiveFormAsync(tenantId, submission.FormId, cancellationToken);
        submission.UpdateData(dataJson);
        await repository.SaveAsync(submission, cancellationToken);

        await PublishCrudEventAsync(form.TenantId, form.Slug, submission.Id, FormOperation.Update, cancellationToken);
        
    }

    public async Task DeleteAsync(
        Guid tenantId,
        Guid submissionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(tenantId, nameof(tenantId));
        ValidateId(submissionId, nameof(submissionId));
        ValidateId(userId, nameof(userId));

        
        await using var repository = await _repositoryFactory.CreateAsync(tenantId, cancellationToken);
        

        var submission = await repository.GetByIdAsync(submissionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Form submission '{submissionId}' not found.");

        var form = await EnsureActiveFormAsync(tenantId, submission.FormId, cancellationToken);

        submission.MarkAsDeleted(userId);
        await repository.SaveAsync(submission, cancellationToken);

        await PublishCrudEventAsync(
            form.TenantId, 
            form.Slug, 
            submission.Id, 
            FormOperation.Delete, 
            cancellationToken);
    }

    
    public async Task<FormSubmission?> GetByIdAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(tenantId, nameof(tenantId));
        ValidateId(id, nameof(id));

        await using var repository = await _repositoryFactory.CreateAsync(tenantId, cancellationToken);

        var submission = await repository.GetByIdAsync(id, cancellationToken);
        if (submission is null)
            return null;

        _ = await EnsureActiveFormAsync(tenantId, submission.FormId, cancellationToken);
        return submission;
    }

    public async Task<FormGridDataDto> GetAllDeletedAsync(
        Guid tenantId, 
        Guid formId, 
        CancellationToken cancellationToken = default)
    {
        ValidateId(tenantId, nameof(tenantId));
        ValidateId(formId, nameof(formId));
        
        await using var repository = await _repositoryFactory.CreateAsync(tenantId, cancellationToken);
        var form = await EnsureActiveFormAsync(tenantId, formId, cancellationToken);
        
        var pagedSubmissions = await repository.GetAllDeletedAsync(
            formId, 
            cancellationToken: cancellationToken);
        
        return await _formFilterService.MapToFormGridDataAsync(
            formId, 
            tenantId, 
            pagedSubmissions, 
            cancellationToken);
        
    }

    public async Task<FormGridDataDto> GetFormGridData(
        FormGridFilterRequest request, 
        CancellationToken cancellationToken)
    {
        ValidateId(request.TenantId, nameof(request.TenantId));
        ValidateId(request.FormId, nameof(request.FormId));
        
        await using var repository = await _repositoryFactory.CreateAsync(request.TenantId, cancellationToken);
        var form = await EnsureActiveFormAsync(request.TenantId, request.FormId, cancellationToken);
        var pagedSubmissions = await repository.GetAllAsync(
            request.FormId, 
            request.PagedRequest,
            cancellationToken: cancellationToken);
        
        return await _formFilterService.MapToFormGridDataAsync(
            request.FormId, 
            request.TenantId, 
            pagedSubmissions, 
            cancellationToken);
    }
    
    public async Task<FormGridDataDto> GetFilteredFormGridData(
        FormGridFilterRequest request, 
        CancellationToken cancellationToken)
    {
        ValidateId(request.TenantId, nameof(request.TenantId));
        ValidateId(request.FormId, nameof(request.FormId));
        await using var repository = await _repositoryFactory.CreateAsync(request.TenantId, cancellationToken);
        
        var form = await EnsureActiveFormAsync(request.TenantId, request.FormId, cancellationToken);
        
        var pagedSubmissions = await repository.GetByFilterAsync(
            request.FormId,
            request.Filters,
            request.PagedRequest,
            cancellationToken: cancellationToken);
        
        return await _formFilterService.MapToFormGridDataAsync(
            request.FormId, 
            request.TenantId, 
            pagedSubmissions, 
            cancellationToken);
    }

    private async Task<FormDto> EnsureActiveFormAsync(
        Guid tenantId,
        Guid formId,
        CancellationToken cancellationToken)
    {
        var form = await _formService.GetFormDtoAsync(formId, tenantId, cancellationToken);
        if (form is null)
        {
            throw new KeyNotFoundException($"Form '{formId}' not found.");
        }

        if (form.TenantId != tenantId)
        {
            throw new KeyNotFoundException($"Form '{formId}' not found for tenant '{tenantId}'.");
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

    private static IEnumerable<FormFieldData> NormalizeFilters(IEnumerable<FormFieldData> filters)
    {
        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.FieldName))
                continue;

            if (filter.Value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
                continue;

            yield return new FormFieldData(
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
