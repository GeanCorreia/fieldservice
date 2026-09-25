using FieldService.Audit.Channels;
using FiledService.Audit.Interfaces;
using FiledService.Audit.Logs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FieldService.Audit.Services;

public class AuditAccessRegistryService : BackgroundService
{
    private readonly ILogger<AuditAccessRegistryService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuditAccessChannel _channel;

    public AuditAccessRegistryService(
        ILogger<AuditAccessRegistryService> logger,
        IServiceScopeFactory scopeFactory,
        AuditAccessChannel channel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var auditAccess in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAuditChangeRepository>();
            var fallbackService = scope.ServiceProvider.GetRequiredService<IAuditFallbackService>();

            try
            {
                await repository.Save(auditAccess);
            }
            catch (Exception ex)
            {
                _logger.LogTrackAccess(
                    LogLevel.Error,
                    auditAccess.ResourceName,
                    auditAccess.ResourceId?.ToString(),
                    ex.Message);

                await fallbackService.AddAuditAccessFallbackAsync(auditAccess, stoppingToken);
            }
        }
    }
}

