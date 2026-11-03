using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.AccountManagement.Dtos.Response;

public record PersonalInfoResponseDto(string FullName, string DisplayName, string Email, string Phone) : AbstractDto
{
    public override bool IsActive => true;
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => nameof(PersonalInfoResponseDto);
    public override string? ResourceId => null;
}

