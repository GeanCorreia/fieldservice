using FieldService.Shared.Types;

namespace FieldService.SignalR.Dtos;

public record DomainRoomRegistryDto(
    UserDto User, 
    Guid RoomId);