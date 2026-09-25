using FiledService.Audit.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FiledService.Audit.Services;

public sealed class AuditDtoSchemaBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<AuditDtoSchemaBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var bootstrapService = scope.ServiceProvider.GetRequiredService<IAuditDtoSchemaBootstrapService>();
            await bootstrapService.EnsureSchemas(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while bootstrapping audit DTO schemas.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

