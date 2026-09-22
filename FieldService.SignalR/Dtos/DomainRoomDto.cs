using FieldService.Shared.Types;

namespace FieldService.SignalR.Dtos;

public record DomainRoomDto(
    Guid RoomId,
    Guid TenantId,
    string Name,
    DomainRoomSettingsDto Settings,
    List<UserTenantDto> Members,
    string? Description = null
    );
    
public record DomainRoomSettingsDto(
);