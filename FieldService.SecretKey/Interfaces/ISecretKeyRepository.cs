namespace FieldService.SecretKey.Interfaces;

internal interface ISecretKeyRepository
{
    Task<Entities.SecretKey?> GetSecretKeyAsync(
        Guid tenantId, string name, CancellationToken cancellationToken = default);
    
    Task<Entities.SecretKey?> GetSecretKeyAsync(Guid secretKeyId, CancellationToken cancellationToken = default);
    
    Task SaveSecretKeyAsync(Entities.SecretKey secretKey, CancellationToken cancellationToken = default);
}