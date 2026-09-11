namespace FieldService.Broker.Interfaces;

public interface IEntityConfigurator
{
    Task ApplyConfigurationAsync(CancellationToken ct = default);
}