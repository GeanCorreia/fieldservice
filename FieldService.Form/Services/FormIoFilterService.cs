using FieldService.Form.Extensions;
using FieldService.Form.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace FieldService.Form.Services;

internal class FormFilterService : IFormFilterService
{
    private readonly IFormService _formService;
    private readonly IFormSubmissionService _formSubmissionService;
    private readonly HybridCache _cache;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);
    
    private const string FormPrefix = "form:";
    private const string FormFilterByIdPrefix = FormPrefix + "form-filter:id:";

    public FormFilterService(
        IFormService formService,
        IFormSubmissionService formSubmissionService,
        HybridCache cache)
    {
        _formService = formService ?? throw new ArgumentNullException(nameof(formService));
        _formSubmissionService = formSubmissionService ?? throw new ArgumentNullException(nameof(formSubmissionService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    private HybridCacheEntryOptions CacheOptions => new()
    {
        Expiration = _cacheTtl,
        LocalCacheExpiration = _cacheTtl
    };
    
    
    public async Task<FormGridFilter?> GetFilterFieldsAsync(Guid formId, CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(
            GetFormFilterByIdKey(formId),
            async ct => await FactoryGetFilterFieldsAsync(formId, ct),
            CacheOptions,
            cancellationToken: cancellationToken);
    }

    public async Task<FormGridData> GetFilteredDataAsync(FormGridFilterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filterDefinition = await GetFilterFieldsAsync(request.FormId, cancellationToken)
            ?? throw new KeyNotFoundException($"Form '{request.FormId}' not found.");

        var submissions = await _formSubmissionService.GetFilteredAsync(request, cancellationToken);

        var rows = submissions.Data
            .Select(submission => new FormGridRow(
                submission.Id,
                submission.LastModifiedAt,
                BuildRowFields(submission.DataJson, filterDefinition.Fields)))
            .ToList();

        return new FormGridData(
            request.FormId,
            new FieldService.Shared.Types.Http.PaginatedResult<FormGridRow>(
                submissions.Pagination,
                rows));
    }


    private async Task<FormGridFilter?> FactoryGetFilterFieldsAsync(Guid formId, CancellationToken cancellationToken)
    {
        var form = await _formService.GetFormDtoAsync(formId, cancellationToken);
        if (form == null)
        {
            return null;
        }
        
        return form.Filter();
    }

                    private static Dictionary<string, object?> BuildRowFields(
                        System.Text.Json.JsonElement dataJson,
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