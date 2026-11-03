using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Request;

/// <param name="Description">Null leaves the description unchanged; an empty string clears it.</param>
public record UpdateJobTitleRequestDto(
    string? Title,
    string? Description) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(UpdateJobTitleRequestDto);
    public override string? ResourceId => null;
}
