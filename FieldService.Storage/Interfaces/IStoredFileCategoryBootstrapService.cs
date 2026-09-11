namespace FieldService.Storage.Interfaces;

internal interface IStoredFileCategoryBootstrapService
{
    Task EnsureCategories(CancellationToken ct = default);
}

