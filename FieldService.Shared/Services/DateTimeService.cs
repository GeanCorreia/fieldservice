using FieldService.Shared.Interfaces;

namespace FieldService.Shared.Services;

public class DateTimeService : IDateTimeService
{
    public DateTimeOffset Now() => GetNow();

    public DateOnly Today() => GetToday();

    public static DateTimeOffset GetNow() => DateTimeOffset.UtcNow;

    public static DateOnly GetToday() => DateOnly.FromDateTime(GetNow().UtcDateTime);
}
