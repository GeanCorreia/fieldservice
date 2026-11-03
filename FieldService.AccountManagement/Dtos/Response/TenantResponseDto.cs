using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Response;

public record TenantResponseDto(
    TenantReferenceResponseDto Tenant,
    IEnumerable<JobTitleResponseDto> JobTitles,
    IEnumerable<TeamResponseDto> Teams) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(TenantResponseDto);
    public override string? ResourceId => Tenant.Id.ToString();
}

