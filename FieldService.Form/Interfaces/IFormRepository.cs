
namespace FieldService.Form.Interfaces;

internal interface IFormRepository
{
    Task<Entities.Form?> GetFormAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<Entities.Form>> GetFormsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task SaveFormAsync(
        Entities.Form form,
        CancellationToken cancellationToken = default);
}