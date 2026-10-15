using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Form.Dtos;

public record FormInfoDto(
    Guid FormId,
    string Title,
    string? Description
) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(FormInfoDto);
    public override string? ResourceId => FormId.ToString();
}

