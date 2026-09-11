using FieldService.Broker.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FieldService.Broker.Services;

public sealed class AzureServiceBusStartupConfigurator : IHostedService
{
    private readonly IBrokerConfigurator _configurator;
    private readonly ILogger<AzureServiceBusStartupConfigurator> _logger;

    public AzureServiceBusStartupConfigurator(
        IBrokerConfigurator configurator,
        ILogger<AzureServiceBusStartupConfigurator> logger)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Azure Service Bus topology provisioning from hosted startup service.");
        await _configurator.Configure(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
