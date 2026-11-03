using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Request;

public record CreateTeamRequestDto(Guid? TeamId, string Name, string? Description, Guid? LeaderId) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(CreateTeamRequestDto);
    public override string? ResourceId => TeamId?.ToString();
}

