using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Response;

public record TeamResponseDto(Guid Id, string Name, string? Description, Guid? LeaderId, string? LeaderName = null, bool IsDeactivated = false) : AbstractDto
{
    public override bool IsActive => !IsDeactivated;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(TeamResponseDto);
    public override string? ResourceId => Id.ToString();
}
