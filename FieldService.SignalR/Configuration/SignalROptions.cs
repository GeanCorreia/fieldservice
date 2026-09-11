namespace FieldService.SignalR.Configuration;

public record SignalROptions
{
    public const string SectionName = "SignalR";
    public int TtlToleranceInSeconds { get; init; } = 30;
};