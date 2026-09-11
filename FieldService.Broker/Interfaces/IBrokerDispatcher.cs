using FieldService.Shared.Message;

namespace FieldService.Broker.Interfaces;

public interface IBrokerDispatcher : IAsyncDisposable
{
   
    Task RegisterConsumersAsync(
        IServiceProvider serviceProvider, 
        CancellationToken ct = default);
    
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    
    bool IsListening(string entityName, string subscriptionName);
    
}