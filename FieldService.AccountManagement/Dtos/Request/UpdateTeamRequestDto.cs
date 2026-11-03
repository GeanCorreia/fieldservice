using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Request;

/// <param name="Description">Null leaves the description unchanged; an empty string clears it.</param>
public record UpdateTeamRequestDto(
    string? Name,
    string? Description,
    Guid? LeaderId) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(UpdateTeamRequestDto);
    public override string? ResourceId => LeaderId?.ToString();
}
