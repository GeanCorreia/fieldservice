using FieldService.Audit.Channels;
using FiledService.Audit.Interfaces;
using FiledService.Audit.Logs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FieldService.Audit.Services;

public class AuditRequestRegistryService : BackgroundService
{
    private readonly ILogger<AuditRequestRegistryService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuditRequestChannel _channel;

    public AuditRequestRegistryService(
        ILogger<AuditRequestRegistryService> logger,
        IServiceScopeFactory scopeFactory,
        AuditRequestChannel channel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var activity in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            if (activity is null)
                continue;
            
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAuditRequestRepository>();
            var service = scope.ServiceProvider.GetRequiredService<IAuditFallbackService>();

            try
            {
                await repository.SaveAsync(activity, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogAuditRequestPersistenceError(
                    LogLevel.Error,
                    activity.Id,
                    ex.Message);
                
                await service.AddSessionActivityFallbackAsync(
                    activity, 
                    stoppingToken);
            }
        }
    }
}