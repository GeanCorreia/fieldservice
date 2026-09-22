using System.Text.Json;

namespace FieldService.SignalR.Events;

internal enum DomainRoomEventType
{
    RoomCreated = 1,
    RoomDeleted = 2,
    UserJoined = 3,
    UserLeft = 4,
    RoomRenamed = 5,
    RoomDescriptionUpdated = 6,
    RoomSettingsUpdated = 7,

}

internal sealed record DomainRoomSettings(
    Guid Id,
    Guid RoomId
    );


internal class DomainRoomEvent
{
    public Guid Id { get; init; }
    public Guid RoomId { get; init; }
    public DateTime OccurredAt { get; init; }
    public Guid CreatedBy { get; init; }
    public DomainRoomEventType EventType { get; init; }
    public Guid? UserId { get; init; }
    public string? RoomName { get; init; }
    public string? RoomDescription { get; init; }
    
    private JsonElement? _roomSettingsJson;
    public DomainRoomSettings? RoomSettings  => _roomSettingsJson.HasValue ? 
        JsonSerializer.Deserialize<DomainRoomSettings>(_roomSettingsJson.Value.GetRawText()) : null;
    
    
    protected DomainRoomEvent()
    {
    }
    
    private DomainRoomEvent(
        Guid id,
        Guid roomId,
        DateTime occurredAt,
        Guid createdBy,
        DomainRoomEventType eventType,
        Guid? userId = null,
        string? roomName = null,
        string? roomDescription = null,
        DomainRoomSettings? roomSettings = null)
    {
        Id = id;
        RoomId = roomId;
        OccurredAt = occurredAt;
        CreatedBy = createdBy;
        EventType = eventType;
        UserId = userId;
        RoomName = roomName;
        RoomDescription = roomDescription;
        _roomSettingsJson = roomSettings is not null ? JsonSerializer.SerializeToElement(roomSettings) : null;
    }

    public static DomainRoomEvent Create(
        Guid roomId,
        Guid createdBy,
        DomainRoomEventType eventType,
        Guid? userId = null,
        string? roomName = null,
        string? roomDescription = null,
        DomainRoomSettings? roomSettings = null
    )
    {
        switch (eventType)
        {
            case DomainRoomEventType.RoomCreated:
                if (string.IsNullOrWhiteSpace(roomName))
                    throw new ArgumentException("Room name is required when creating a room.", nameof(roomName));

                if (string.IsNullOrWhiteSpace(roomDescription))
                    throw new ArgumentException("Room description is required when creating a room.", nameof(roomDescription));

                if (roomSettings is null)
                    throw new ArgumentNullException(nameof(roomSettings), "Room settings are required when creating a room.");
                break;
            
            case DomainRoomEventType.RoomDeleted:
                if (!string.IsNullOrWhiteSpace(roomName))
                    throw new ArgumentException("Room name must not be provided when deleting a room.", nameof(roomName));

                if (!string.IsNullOrWhiteSpace(roomDescription))
                    throw new ArgumentException("Room description must not be provided when deleting a room.", nameof(roomDescription));

                if (roomSettings is not null)
                    throw new ArgumentNullException(nameof(roomSettings), "Room settings must not be provided when deleting a room.");
                break;

            case DomainRoomEventType.RoomRenamed when string.IsNullOrWhiteSpace(roomName):
                throw new ArgumentException("Room name must be provided when renaming a room.", nameof(roomName));

            case DomainRoomEventType.RoomDescriptionUpdated when string.IsNullOrWhiteSpace(roomDescription):
                throw new ArgumentException("Room description must be provided when updating room description.", nameof(roomDescription));

            case DomainRoomEventType.RoomSettingsUpdated when roomSettings is null:
                throw new ArgumentNullException(nameof(roomSettings), "Room settings must be provided when updating room settings.");

            case DomainRoomEventType.UserJoined or DomainRoomEventType.UserLeft when userId is null || userId == Guid.Empty:
                throw new ArgumentException("UserId must be provided for user join/left events.", nameof(userId));
            
        }
        
        return new DomainRoomEvent(
            id: Guid.NewGuid(),
            roomId: roomId,
            occurredAt: DateTime.UtcNow,
            createdBy: createdBy,
            eventType: eventType,
            userId: userId,
            roomName: roomName,
            roomDescription: roomDescription,
            roomSettings: roomSettings
        );
    }
}