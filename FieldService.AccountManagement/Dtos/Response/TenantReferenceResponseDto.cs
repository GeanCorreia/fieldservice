using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Response;

public record TenantReferenceResponseDto(Guid Id, string Name) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(TenantReferenceResponseDto);
    public override string? ResourceId => Id.ToString();
}

