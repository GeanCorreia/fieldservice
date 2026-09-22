namespace FieldService.Superset.Interfaces;

public interface IDatabaseService
{
    void ExecuteCommand(string sqlCommand, object? parameters = null);
    Task ExecuteCommandAsync(string sqlCommand, object? parameters = null);
}