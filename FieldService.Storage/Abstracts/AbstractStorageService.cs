using FieldService.Data.Interfaces;
using FieldService.Shared.Services;
using FieldService.Storage.Channels;
using FieldService.Storage.Configuration;
using FieldService.Storage.Data;
using FieldService.Storage.Dtos;
using FieldService.Storage.Entities;
using FieldService.Storage.Factories;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Services;
using FieldService.Storage.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Abstracts;

public class AbstractStorageService
{
    protected readonly ISqlUnitOfWork<StorageDbContext> _unitOfWork;
    protected readonly StorageProviderFactory _storageProviderFactory;
    protected readonly int _expiryPreSignedUrlMinutes;
    protected readonly StorageProvider _defaultStorageProvider;
    protected readonly IStoredFileService _storedFileService;
    protected readonly int _maxParallelism;
    protected readonly StoredFileFailedUploadOutboxChannel _brokerMessageChannel;
    protected readonly IServiceProvider _serviceProvider;
    
    public AbstractStorageService(
        StorageProviderFactory storageProviderFactory,
        IConfiguration configuration,
        IStoredFileService storedFileService,
        ISqlUnitOfWork<StorageDbContext> unitOfWork,
        IServiceProvider serviceProvider)
    {
        _storageProviderFactory = storageProviderFactory ?? throw new ArgumentNullException(nameof(storageProviderFactory));
        _storedFileService = storedFileService ?? throw new ArgumentNullException(nameof(storedFileService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        
        configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        var storageOptions = configuration
                                 .GetSection(StorageOptions.SectionName)
                                 .Get<StorageOptions>()
                             ?? throw new InvalidOperationException($"Configuration section '{StorageOptions.SectionName}' was not found or is invalid.");

        _expiryPreSignedUrlMinutes = storageOptions.ExpiryPreSignedUrlMinutes;
        _defaultStorageProvider = storageOptions.DefaultStorageProvider;
        _maxParallelism = storageOptions.MaxParallelism;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected TimeSpan ExpiryPreSignedUrl => TimeSpan.FromMinutes(_expiryPreSignedUrlMinutes);

    protected IStorageProviderService GetStorageProvider(StorageProvider? provider = null)
    {
        return provider.HasValue
            ? _storageProviderFactory.GetProvider(provider.Value)
            : _storageProviderFactory.GetProvider(_defaultStorageProvider);
    }
    
    
    
}