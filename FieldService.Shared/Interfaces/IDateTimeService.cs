namespace FieldService.Shared.Interfaces;

public interface IDateTimeService
{
    public DateTimeOffset Now();
    public DateOnly Today();
    
}