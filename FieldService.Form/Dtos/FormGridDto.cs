using FieldService.Shared.Dtos;
using FieldService.Shared.Types;
using FieldService.Shared.Types.Http;

namespace FieldService.Form.Dtos;

public record FormField(
    string FieldName,
    string Label,
    FormDataType FieldType,
    string? InputMask = null,
    string? Placeholder = null
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

public record FormGridDataDto(
    Guid FormId,
    PaginatedResult<FormGridRow> Data) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(FormGridDataDto);
    public override string? ResourceId => FormId.ToString();
}

public record FormGridFilterRequest(
    Guid TenantId,
    Guid FormId,
    IEnumerable<FormFieldData> Filters,
    PagedRequest? PagedRequest = null);

