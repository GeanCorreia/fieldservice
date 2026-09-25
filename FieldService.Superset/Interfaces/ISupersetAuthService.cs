using FieldService.SecretKey.Entities;
using FieldService.Superset.Dtos;
using FieldService.Superset.Services;
using MongoDB.Driver.Core.Configuration;

namespace FieldService.Superset.Interfaces;

internal interface ISupersetAuthService
{
    Task<string> GetAdminTokenApi(
        string fqdnUrl, 
        CancellationToken cancellationToken = default);
    
    Task<string> GetDatabaseConnectionString(
        Guid connectionStringId, 
        CancellationToken cancellationToken = default);
    
    Task<string> GetSupersetSecretApiKey(
        Guid apiKeyId, 
        CancellationToken cancellationToken = default);
    
    Task<(Guid KeyId, string Key)> CreateSupersetSecretApiKey(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
    
    Task<(Guid ConnectionStringId, SupersetDatabaseParams databaseParams, string ConnectionString)> CreateDatabaseConnectionString(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}