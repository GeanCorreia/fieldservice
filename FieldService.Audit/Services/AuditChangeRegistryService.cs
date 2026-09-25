using FieldService.Audit.Channels;
using FiledService.Audit.Interfaces;
using FiledService.Audit.Logs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FieldService.Audit.Services;

public class AuditChangeRegistryService : BackgroundService
{
    private readonly ILogger<AuditChangeRegistryService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuditChangeChannel _channel;

    public AuditChangeRegistryService(
        ILogger<AuditChangeRegistryService> logger,
        IServiceScopeFactory scopeFactory,
        AuditChangeChannel channel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var auditChange in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAuditChangeRepository>();
            var fallbackService = scope.ServiceProvider.GetRequiredService<IAuditFallbackService>();

            try
            {
                await repository.Save(auditChange);
            }
            catch (Exception ex)
            {
                _logger.LogTrackPersist(LogLevel.Error, ex.Message);

                await fallbackService.AddAuditChangeFallbackAsync(auditChange, stoppingToken);
            }
        }
    }
}


