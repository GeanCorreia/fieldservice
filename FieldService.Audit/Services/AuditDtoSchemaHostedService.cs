using FiledService.Audit.Data;
using FiledService.Audit.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FiledService.Audit.Services;

public sealed class AuditDtoSchemaHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var bootstrapService = scope.ServiceProvider.GetRequiredService<IAuditDtoSchemaBootstrapService>();
        await bootstrapService.EnsureSchemas(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
