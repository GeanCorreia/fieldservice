namespace FieldService.Form.Services;

using System.Text.Json;
using FieldService.Form.Dtos;
using FieldService.Form.Entities;
using FieldService.Form.Extensions;
using FieldService.Form.Interfaces;
using FieldService.Shared.Types.Http;
using Microsoft.Extensions.Caching.Hybrid;

internal class FormIoFilterService : IFormFilterService
{
    private readonly IFormSubmissionRepositoryFactory _repositoryFactory;
    private readonly IFormService _formService;
    private readonly HybridCache _cache;

    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);
    
    private const string FormPrefix = "form:";
    private const string FormFilterByIdPrefix = FormPrefix + "form-filter:id:";

    public FormIoFilterService(
        IFormService formService,
        IFormSubmissionRepositoryFactory repositoryFactory,
        HybridCache cache)
    {
        _formService = formService ?? throw new ArgumentNullException(nameof(formService));
        _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    private HybridCacheEntryOptions CacheOptions => new()
    {
        Expiration = _cacheTtl,
        LocalCacheExpiration = _cacheTtl
    };
    
    public async Task<FormGridDataDto> MapToFormGridDataAsync(
        Guid formId,
        Guid tenantId,
        PaginatedResult<FormSubmission> pagedSubmissions,
        CancellationToken cancellationToken)
    {

        var filterFields = await GetFilterFieldsAsync(formId, tenantId, cancellationToken);
        if (filterFields == null)
        {
            throw new InvalidOperationException($"Não foi possível carregar as definições de filtro do formulário '{formId}'.");
        }
        
        var rows = pagedSubmissions.Data.Select(submission => new FormGridRow(
            SubmissionId: submission.Id,
            ModifiedAt: submission.LastModifiedAt,
            Fields: BuildRowFields(submission.DataJson, filterFields.Fields)
        )).ToList();
        
        var pagination = new Pagination(
            Page: pagedSubmissions.Pagination.Page,
            PageSize: pagedSubmissions.Pagination.PageSize,
            TotalRecords: pagedSubmissions.Pagination.TotalRecords,
            TotalPages: pagedSubmissions.Pagination.TotalPages
        );
        
        var pagedRows = new PaginatedResult<FormGridRow>(
            Pagination: pagination,
            Data: rows
        );

        return new FormGridDataDto(formId, pagedRows);
    }

    private async Task<FormGridFilter?> GetFilterFieldsAsync(Guid formId, Guid tenantId, CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(
            GetFormFilterByIdKey(formId),
            async ct => await FactoryGetFilterFieldsAsync(formId, tenantId, ct),
            CacheOptions,
            cancellationToken: cancellationToken);
    }

    private async Task<FormGridFilter?> FactoryGetFilterFieldsAsync(Guid formId, Guid tenantId, CancellationToken cancellationToken)
    {
        var form = await _formService.GetFormDtoAsync(formId, tenantId, cancellationToken);
        if (form == null)
        {
            return null;
        }
        
        return form.Filter();
    }

    private static Dictionary<string, object?> BuildRowFields(
        JsonElement dataJson,
        IEnumerable<FormField> fields)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            result[field.FieldName] = dataJson.TryGetValueByPath(field.FieldName, out var value)
                ? value.ToObjectValue()
                : null;
        }

        return result;
    }
    private static string GetFormFilterByIdKey(Guid formId) => FormFilterByIdPrefix + formId;

 
}