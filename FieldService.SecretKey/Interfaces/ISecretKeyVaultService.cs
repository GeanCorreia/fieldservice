namespace FieldService.SecretKey.Interfaces;

internal interface ISecretKeyVaultService
{
    Task<T?> GetSecretKeyAsync<T>(
        Guid tenantId, 
        string name, 
        CancellationToken cancellationToken = default) 
        where T : ISecretKeyType;

    Task SetSecretKeyAsync<T>(
        Guid tenantId,
        string name, 
        T secretKey, 
        CancellationToken cancellationToken = default) 
        where T : ISecretKeyType;

    Task RemoveSecretKeyAsync(
        Guid tenantId,
        string name, 
        CancellationToken cancellationToken = default);

    Task EvictFromServiceBusAsync(
        string secretName,
        CancellationToken cancellationToken = default);
}