using FieldService.Form.Entities;
using FieldService.Form.Interfaces;
using FieldService.SecretKey.Cqrs.Queries.GetSecretKey;
using FieldService.SecretKey.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Form.Data;

internal sealed class FormDataDbContextFactory : IFormDataDbContextFactory
{
    private readonly IMediator _mediator;

    public FormDataDbContextFactory(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task<DbContext> GetFormDataDbContextAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == default)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        var tenantConnectionString = await GetTenantConnectionStringAsync(tenantId, cancellationToken);

        var optionsBuilder = new DbContextOptionsBuilder<FormDataDbContext>()
            .UseNpgsql(tenantConnectionString);

        return new FormDataDbContext(optionsBuilder.Options);
    }

    private async Task<string> GetTenantConnectionStringAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var connectionStringSecret = await _mediator
                .Send(new GetSecretKeyQuery<ConnectionStringSecret>(tenantId, FormTenant.FormSecretName(tenantId)), cancellationToken);

            if (string.IsNullOrWhiteSpace(connectionStringSecret.ConnectionString))
            {
                throw new InvalidOperationException(
                    $"No valid connection string found for form tenant '{tenantId}' with secret name '{FormTenant.FormSecretName(tenantId)}'.");
            }

            return connectionStringSecret.ConnectionString;
        }
        catch (KeyNotFoundException ex)
        {
            throw new InvalidOperationException(
                $"No valid connection string found for form tenant '{tenantId}' with secret name '{FormTenant.FormSecretName(tenantId)}'.",
                ex);
        }
    }
}

