namespace FieldService.Shared.Interfaces;

public interface IDateTimeService
{
    public DateTime Now();
    public DateOnly Today();
    
}