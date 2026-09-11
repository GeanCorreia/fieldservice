using FieldService.Broker.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FieldService.Broker.Workers;

public sealed class BrokerDispatcherWorker : BackgroundService
{
    private readonly IBrokerDispatcher _dispatcher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BrokerDispatcherWorker> _logger;

    public BrokerDispatcherWorker(
        IBrokerDispatcher dispatcher,
        IServiceProvider serviceProvider,
        ILogger<BrokerDispatcherWorker> logger)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BrokerWorker] Discovering and registering consumers...");
        await _dispatcher.RegisterConsumersAsync(_serviceProvider, stoppingToken);
        await _dispatcher.StartAsync(stoppingToken);
        _logger.LogInformation("[BrokerWorker] Broker Dispatcher is up and listening.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[BrokerWorker] Stopping Broker Dispatcher...");
        
        await _dispatcher.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}