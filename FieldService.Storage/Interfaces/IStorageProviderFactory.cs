using FieldService.Storage.Entities;

namespace FieldService.Storage.Interfaces;

public interface IStorageProviderFactory
{
    IStorageProviderService GetProvider(StorageProvider provider);
}