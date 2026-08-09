namespace FieldService.Shared.Types;

public sealed record Interval(
    DateTimeOffset Start,
    DateTimeOffset? End = null)
{
    public bool IsOpen => End is null;

    public bool IsClosed => End is not null;

    public bool Contains(DateTimeOffset instant)
        => instant >= Start &&
           (End is null || instant <= End);

    public bool HasEnded(DateTimeOffset now)
        => End is not null && now >= End;

    public bool IsActive(DateTimeOffset now)
        => now >= Start &&
           (End is null || now <= End);

    public TimeSpan? Duration =>
        End is null ? null : End.Value - Start;

    public Interval Close(DateTimeOffset end)
    {
        if (end < Start)
            throw new ArgumentException("End must be greater than Start.");

        return this with { End = end };
    }
}