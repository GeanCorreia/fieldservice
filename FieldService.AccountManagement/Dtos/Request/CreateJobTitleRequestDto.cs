using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Request;

public record CreateJobTitleRequestDto(Guid? JobTitleId, string Title, string? Description) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(CreateJobTitleRequestDto);
    public override string? ResourceId => JobTitleId?.ToString();
}

