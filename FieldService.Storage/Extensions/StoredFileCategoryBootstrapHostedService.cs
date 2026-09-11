using FieldService.Storage.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FieldService.Storage.Extensions;

internal sealed class StoredFileCategoryBootstrapHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var bootstrapService = scope.ServiceProvider.GetRequiredService<StoredFileCategoryBootstrapService>();
        await bootstrapService.EnsureCategories(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

