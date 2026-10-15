using FieldService.Data.Interfaces;
using FieldService.Form.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FieldService.Form.Data.Repositories;

internal sealed class FormSubmissionRepositoryFactory(
    IServiceProvider serviceProvider,
    IFormDataDbContextFactory formDataDbContextFactory) : IFormSubmissionRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IFormDataDbContextFactory _formDataDbContextFactory = formDataDbContextFactory ?? throw new ArgumentNullException(nameof(formDataDbContextFactory));

    public async Task<IFormSubmissionRepository> CreateAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await _formDataDbContextFactory.GetFormDataDbContextAsync(tenantId, cancellationToken);

        var typedDbContext = dbContext as FormDataDbContext
            ?? throw new InvalidOperationException($"Expected DbContext of type '{nameof(FormDataDbContext)}'.");

        var sqlUnitOfWorkType = typeof(ISqlUnitOfWork<FormDataDbContext>).Assembly
            .GetType("FieldService.Data.Services.SqlUnitOfWork`1")?
            .MakeGenericType(typeof(FormDataDbContext))
            ?? throw new InvalidOperationException("Could not resolve SqlUnitOfWork for FormDataDbContext.");

        var unitOfWork = ActivatorUtilities.CreateInstance(_serviceProvider, sqlUnitOfWorkType, typedDbContext) as ISqlUnitOfWork<FormDataDbContext>
            ?? throw new InvalidOperationException($"Could not create {nameof(ISqlUnitOfWork<FormDataDbContext>)}.");

        return new FormSubmissionRepository(
            typedDbContext,
            unitOfWork);
    }
}

