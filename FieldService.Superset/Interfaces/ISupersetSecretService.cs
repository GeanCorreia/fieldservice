using FieldService.SecretKey.Entities;
using FieldService.Shared.Types;
using FieldService.Superset.Dtos;
using FieldService.Superset.Services;
using MongoDB.Driver.Core.Configuration;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetSecretService
{
   
   
    Task<string?> GetDatabaseConnectionString(
        Guid connectionStringId, 
        CancellationToken cancellationToken = default);
    
    Task<string?> GetSupersetSecretApiKey(
        Guid apiKeyId, 
        CancellationToken cancellationToken = default);
    
    Task<(Guid KeyId, string Key)> CreateSupersetSecretApiKey(
        Guid tenantId,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
    
    Task<(
        Guid ConnectionStringId, 
        SupersetDatabaseParams databaseParams, 
        string ConnectionString)> 
        CreateDatabaseConnectionString(
        Guid tenantId,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
    
   
    

}