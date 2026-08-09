using FieldService.Shared.Interfaces;

namespace FieldService.Shared.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime Now() => GetNow();

    public DateOnly Today() => GetToday();

    public static DateTime GetNow() => DateTime.UtcNow;

    public static DateOnly GetToday() => DateOnly.FromDateTime(GetNow());

    public static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static DateTime? EnsureUtc(DateTime? value)
    {
        return value.HasValue ? EnsureUtc(value.Value) : null;
    }
}
