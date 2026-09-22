using FieldService.Shared.Types;
using FieldService.SignalR.Events;

namespace FieldService.SignalR.Entities;

internal class DomainRoom
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    private List<DomainRoomEvent> _events = new List<DomainRoomEvent>();
    internal IReadOnlyCollection<DomainRoomEvent> Events => _events.AsReadOnly();
    public IReadOnlyCollection<Guid> UserMemberIds => _events
        .Where(e => e.EventType == DomainRoomEventType.UserJoined || e.EventType == DomainRoomEventType.UserLeft)
        .GroupBy(e => e.UserId)
        .Select(g => g.OrderByDescending(ev => ev.OccurredAt).First())
        .Where(lastEvent => lastEvent.EventType == DomainRoomEventType.UserJoined)
        .Select(lastEvent => lastEvent.UserId!.Value)
        .ToList()
        .AsReadOnly();
    
    
    protected DomainRoom()
    {
    }
    
    private DomainRoom(
        Guid id,
        Guid tenantId,
        ICollection<DomainRoomEvent> events)
    {
        Id = id;
        TenantId = tenantId;
        _events = events.ToList();

    }
    
    private static DomainRoomEvent CreateDomainRoomEvent( 
        Guid roomId,
        Guid createdBy,
        DomainRoomEventType eventType,
        Guid? userId = null,
        string? roomName = null,
        string? roomDescription = null,
        DomainRoomSettings? roomSettings = null)
    {
        return DomainRoomEvent.Create(
            roomId,
            createdBy,
            eventType,
            userId,
            roomName,
            roomDescription,
            roomSettings);
    }

    public static DomainRoom CreateRoom(
        Guid tenantId,
        Guid roomId,
        string name,
        string description,
        Guid createdBy,
        DomainRoomSettings domainRoomSettings,
        IList<UserTenantDto>? members = null)
    {
        
        var createdEvent = CreateDomainRoomEvent(
            roomId,
            createdBy,
            DomainRoomEventType.RoomCreated,
            userId: null,
            name,
            description,
            domainRoomSettings
        );
        
        return new DomainRoom(
            roomId,
            tenantId,
            new List<DomainRoomEvent> { createdEvent }
        );
   
    }
        
    
    public void AddMember(UserTenantDto member)
    {
        if (UserMemberIds.Contains(member.Id))
        {
            return;
        }
        
        var userJoinedEvent = CreateDomainRoomEvent(
            Id,
            member.Id,
            DomainRoomEventType.UserJoined,
            userId: member.Id
        );
        
        _events.Add(userJoinedEvent);
    }

    public void RemoveMember(Guid userId)
    {
        if (!UserMemberIds.Contains(userId))
        {
            return;
        }
        
        var userLeftEvent = CreateDomainRoomEvent(
            Id,
            userId,
            DomainRoomEventType.UserLeft,
            userId: userId
        );
        _events.Add(userLeftEvent);
    }
    
    public void AddDescription(
        string description, 
        Guid updatedBy)
    {
        var descriptionUpdatedEvent = CreateDomainRoomEvent(
            Id,
            updatedBy,
            DomainRoomEventType.RoomDescriptionUpdated,
            userId: null,
            roomDescription: description
        );
        
        _events.Add(descriptionUpdatedEvent);
    }
    
    public void UpdateSettings(
        DomainRoomSettings settings, 
        Guid updatedBy)
    {
        var settingsUpdatedEvent = CreateDomainRoomEvent(
            Id,
            updatedBy,
            DomainRoomEventType.RoomSettingsUpdated,
            userId: null,
            roomSettings: settings
        );
        
        _events.Add(settingsUpdatedEvent);
    }
    
    public void UpdateName(
        string name, 
        Guid updatedBy)
    {
        var nameUpdatedEvent = CreateDomainRoomEvent(
            Id,
            updatedBy,
            DomainRoomEventType.RoomRenamed,
            userId: null,
            roomName: name
        );
        
        _events.Add(nameUpdatedEvent);
    }
    
    public bool HasMember(Guid userId)
    {
        return UserMemberIds.Contains(userId);
    }
}