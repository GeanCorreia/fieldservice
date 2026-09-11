using FieldService.Data.Interfaces;
using FieldService.Shared.Services;
using FieldService.Storage.Channels;
using FieldService.Storage.Configuration;
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
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly StorageProviderFactory _storageProviderFactory;
    protected readonly int _expiryPreSignedUrlMinutes;
    protected readonly StorageProvider _defaultStorageProvider;
    protected readonly IStoredFileService _storedFileService;
    protected readonly int _maxParallelism;
    protected readonly ILogger<StorageService> _logger;
    protected readonly StoredFileOutboxChannel _brokerMessageChannel;
    
    public AbstractStorageService(
        StorageProviderFactory storageProviderFactory,
        IConfiguration configuration,
        IStoredFileService storedFileService,
        IUnitOfWork unitOfWork,
        ILogger<StorageService> logger,
        StoredFileOutboxChannel brokerMessageChannel)
    {
        _storageProviderFactory = storageProviderFactory ?? throw new ArgumentNullException(nameof(storageProviderFactory));
        _storedFileService = storedFileService ?? throw new ArgumentNullException(nameof(storedFileService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        var storageOptions = configuration
                                 .GetSection(StorageOptions.SectionName)
                                 .Get<StorageOptions>()
                             ?? throw new InvalidOperationException($"Configuration section '{StorageOptions.SectionName}' was not found or is invalid.");

        _expiryPreSignedUrlMinutes = storageOptions.ExpiryPreSignedUrlMinutes;
        _defaultStorageProvider = storageOptions.DefaultStorageProvider;
        _maxParallelism = storageOptions.MaxParallelism;
        _brokerMessageChannel = brokerMessageChannel ?? throw new ArgumentNullException(nameof(brokerMessageChannel));
    }

    protected TimeSpan ExpiryPreSignedUrl => TimeSpan.FromMinutes(_expiryPreSignedUrlMinutes);

    protected IStorageProviderService GetStorageProvider(StorageProvider? provider = null)
    {
        return provider.HasValue
            ? _storageProviderFactory.GetProvider(provider.Value)
            : _storageProviderFactory.GetProvider(_defaultStorageProvider);
    }
    
    
    
}