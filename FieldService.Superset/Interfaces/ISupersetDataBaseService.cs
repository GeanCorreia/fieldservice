using System.Data.Common;
using FieldService.Superset.Dtos;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetDataBaseService
{
    Task EnsureSupersetDataInfraCheckpointAsync(
        Guid tenantId,
        Guid connectionStringId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateSupersetDataInfra(
        Guid tenantId,
        Guid? customHostConnectionStringId = null,
        CancellationToken cancellationToken = default);
    
}