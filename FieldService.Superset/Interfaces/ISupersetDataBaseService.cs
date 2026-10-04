using System.Data.Common;
using FieldService.Superset.Dtos;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetDataBaseService
{
    Task<Guid> CreateSupersetDataInfra(
        Guid tenantId,
        DbConnectionStringBuilder? dedicatedDbConnectionString = null,
        CancellationToken cancellationToken = default);
    
}