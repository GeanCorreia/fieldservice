using FieldService.Shared.Types.Http;

namespace FieldService.Form.Interfaces;


public record FormField(
    string FieldName,
    string Label,
    FormDataType FieldType,
    string? InputMask = null,      // Ex: "999.999.999-99" ou "(99) 99999-9999"
    string? Placeholder = null     // Ex: "___.___.___-__"
    
);

public enum FormDataType
{
    String,
    Number,
    Boolean,
    Date,
    Object,
    Array
}

public record FormGridFilter(
    Guid FormId,
    List<FormField> Fields);

public record FormFieldData(
    string FieldName,
    object? Value);

public record FormGridRow(
    Guid SubmissionId,
    DateTimeOffset ModifiedAt,
    Dictionary<string, object?> Fields);

public record FormGridData(
    Guid FormId,
    PaginatedResult<FormGridRow> Data);

public record FormGridFilterRequest(
    Guid FormId,
    IEnumerable<FormFieldData> Filters,
    PagedRequest? PagedRequest = null);

 
internal interface IFormFilterService
{
    Task<FormGridFilter?> GetFilterFieldsAsync(
        Guid formId,
        CancellationToken cancellationToken);
    
    Task<FormGridData> GetFilteredDataAsync(
        FormGridFilterRequest request,
        CancellationToken cancellationToken);
    
}