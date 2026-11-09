using FieldService.Cloud.Entities;

namespace FieldService.Cloud.Interfaces;

public interface IContainerRepository
{
    Task<Container?> GetByIdAsync(Guid containerId, CancellationToken cancellationToken = default);

    Task<Container?> GetByTenantAndNameAsync(Guid tenantId, string resourceName,
        CancellationToken cancellationToken = default);

    Task SaveAsync(Container container, CancellationToken cancellationToken = default);
}