namespace FieldService.Form.Interfaces;

internal interface IFormDatabaseService
{
    Task<Guid> CreateFormDataInfraAsync(
        Guid tenantId,
        Guid? customHostConnectionStringId = null,
        CancellationToken cancellationToken = default);
    
    Task EnsureFormDataInfraCheckpointAsync(
        Guid tenantId,
        Guid connectionStringId,
        CancellationToken cancellationToken = default);
}