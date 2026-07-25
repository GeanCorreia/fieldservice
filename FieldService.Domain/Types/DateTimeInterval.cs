namespace FieldService.Domain.Types;

public class DateTimeInterval
{
    public DateTime Start { get; }
    public DateTime? End { get; private set; }
    
    protected DateTimeInterval() { }

    public DateTimeInterval(DateTime start, DateTime? end = null)
    {
        if (end.HasValue && start >= end.Value)
            throw new ArgumentException("Start must be before End.", nameof(start));
        
        Start = start;
        End = end;
    }
    
    public void EndedAt(DateTime end)
    {
        if (end <= Start)
            throw new ArgumentException("End must be after Start.", nameof(end));
        
        if (End.HasValue)
            throw new InvalidOperationException("Interval already has an end date.");
        
        End = end;
    }
    
    public bool IsActive() => !End.HasValue;
    
    public bool WasActiveAt(DateTime moment)
    {
        return moment >= Start && (!End.HasValue || moment < End.Value);
    }
    
    public bool WasActiveDuring(DateTime periodStart, DateTime periodEnd)
    {
        if (periodStart >= periodEnd)
            throw new ArgumentException("Period start must be before period end.");
        
        return Start < periodEnd && (!End.HasValue || End.Value > periodStart);
    }
    
    public TimeSpan GetDuration()
    {
        if (!End.HasValue)
            throw new InvalidOperationException("Cannot calculate duration for an ongoing interval.");
        
        return End.Value - Start;
    }
}