using FieldService.Shared.Dtos;
using FieldService.Shared.Types;

namespace FieldService.Superset.Dtos;

public record SupersetTokenResponse(
    string Token);
    
/// <summary>
/// Token de convidado pronto para ser consumido pelo frontend
/// </summary>
public record GuestTokenResponse(
    string Token,
    DateTime Expiration
);

/// <summary>
/// Representação simplificada de um Dashboard no domínio da sua aplicação
/// </summary>
public record DashboardSummaryResponse(
    int Id,
    string Title,
    string? EmbeddedUuid,
    bool IsPublished
);

public record SupersetUserDetailResponseDto(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    bool Active,
    List<Permission> Permissions
) : AbstractDto
{
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => "SupersetUserDetailResponseDto";
    public override string? ResourceId => Username;
    public override bool IsActive => Active;
};

public record SupersetUserListResponseDto(
    Guid TenantId,
    List<SupersetUserDetailResponseDto> Users
) : AbstractDto
{
    public override SchemaVersion Version => new(1, 0, 0);
    public override string ResourceName => "SupersetUserListResponseDto";
    public override string? ResourceId => TenantId.ToString();
    public override bool IsActive => true;
};