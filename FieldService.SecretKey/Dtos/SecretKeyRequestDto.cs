using FieldService.SecretKey.Entities;
using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.SecretKey.Dtos;

public record SecretKeyRequestDto(
    SecretKeyDto SecretKeyDto) : AbstractDto
{
    public override SchemaVersion Version => new(1, 1, 0);
    public override string ResourceName => "SecretKeyRequestDto";
    public override Guid? ResourceId => null;
    public override bool IsActive => false;
}

public record SecretKeyDeleteRequestDto(
    Guid TenantId,
    Guid SecretName) : AbstractDto
{
    public override SchemaVersion Version => new(1, 1, 0);
    public override string ResourceName => "SecretKeyDeleteRequestDto";
    public override Guid? ResourceId => SecretName;
    public override bool IsActive => false;
}

public record SecretKeyUpdateRequestDto(
    SecretKeyType? Type,
    string? NewName
    ) : AbstractDto
{
    public override SchemaVersion Version => new(1, 1, 0);
    public override string ResourceName => "SecretKeyUpdateRequestDto";
    public override Guid? ResourceId => null;
    public override bool IsActive => false;
}