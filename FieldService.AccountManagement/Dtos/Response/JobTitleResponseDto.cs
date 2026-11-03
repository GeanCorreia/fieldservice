using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Response;

public record JobTitleResponseDto(Guid Id, string Title, string? Description, bool IsDeactivated = false) : AbstractDto
{
    public override bool IsActive => !IsDeactivated;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(JobTitleResponseDto);
    public override string? ResourceId => Id.ToString();
}
