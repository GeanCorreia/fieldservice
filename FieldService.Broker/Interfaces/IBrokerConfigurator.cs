namespace FieldService.Broker.Interfaces;

public interface IBrokerConfigurator
{
     Task Configure(CancellationToken cancellationToken = default);
}