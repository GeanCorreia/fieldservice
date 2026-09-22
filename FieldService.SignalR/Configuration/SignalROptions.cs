namespace FieldService.SignalR.Configuration;

public record SignalROptions
{
    public const string SectionName = "SignalR";
    public int TtlToleranceInSeconds { get; init; } = 30;
    public int TtlRoomRegistryInHours { get; init; } = 24;
    public int TtlPresenceRegistryInHours{ get; init; } = 24;
    public int TtlLocalExpirationInMinutes { get; init; } = 30;
};