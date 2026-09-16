using FieldService.Broker.Interfaces;
using FieldService.Broker.Message;
using FieldService.Shared.Message;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Broker;


internal class AzureStorageSuccessUploadMessageConsumer : IBrokerConsumer<AzureEventGridBlobCreatedPayload>
{
    private readonly ILogger<AzureStorageSuccessUploadMessageConsumer> _logger;
    private readonly IStoredFileService _storedFileService;
    private readonly StoredFileUploadedMessageProducer _storedFileUploadedMessageProducer;
    private readonly IStoredFileRepository _storedFileRepository;
    private readonly IStorageProviderService _azureStorageProviderService;

    public AzureStorageSuccessUploadMessageConsumer(
        ILogger<AzureStorageSuccessUploadMessageConsumer> logger,
        IStoredFileService storedFileService,
        StoredFileUploadedMessageProducer storedFileUploadedMessageProducer,
        IStorageProviderFactory storageProviderFactory,
        IStoredFileRepository storedFileRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storedFileService = storedFileService ?? throw new ArgumentNullException(nameof(storedFileService));
        _storedFileUploadedMessageProducer = storedFileUploadedMessageProducer ?? 
                                             throw new ArgumentNullException(nameof(storedFileUploadedMessageProducer));
        _azureStorageProviderService = storageProviderFactory.GetProvider(
            StorageProvider.AzureBlob) ?? throw new ArgumentNullException(nameof(storageProviderFactory));
        
        _storedFileRepository = storedFileRepository ?? throw new ArgumentNullException(nameof(storedFileRepository));
    }
    private static string EntityName => AzureStorageSuccessUploadEnvelopeMessage.EnvelopeContext.EntityName;
    private static string SubscriptionName => "storage.azure-success-upload.sub";
    
    public static BrokerSubscribeContext BrokerSubscriptionContext { get; } = new(
        EntityName: EntityName,
        SubscriptionName: SubscriptionName
    );
    
    
    public async Task ConsumeAsync(
        IMessage<AzureEventGridBlobCreatedPayload> message, 
        CancellationToken ct = default)
    {
        var fileId = message.Payload?.FileId;
        if (fileId is null)
        {
            var exception = new InvalidOperationException(
                $"Invalid message for {nameof(AzureStorageSuccessUploadMessageConsumer)}. " +
                $"Payload is null.");
            
            _logger.LogOutboxServiceError(
                LogLevel.Error,
                message.Payload.Subject,
                StorageProvider.AzureBlob,
                exception.Message,
                exception
            );
             throw exception;
        }

        try
        {
            await _storedFileService.UpdateUploadedStatusAsync(fileId.Value, ct);
        }
        catch 
        {
            _logger.LogOutboxServiceError(
                LogLevel.Error,
                message.Payload.Subject,
                StorageProvider.AzureBlob,
                $"Error updating upload status for file with id {fileId.Value}."
            );
            throw;
        
        }

        try
        {
            var file = await _storedFileRepository.GetByIdAsync(fileId.Value, ct);

            if (file is null)
            {
                var exception = new InvalidOperationException(
                    $"File {fileId.Value} not found.");
                
               
                throw exception;
            }
            
            var azureBlobProperties = await _azureStorageProviderService.GetBlobPropertiesAsync(file, ct);
            var azureMd5Base64 = Convert.ToBase64String(azureBlobProperties.Value.ContentHash);
            if (azureMd5Base64 != file.HashMd5 || azureBlobProperties.Value.ContentLength != file.Size)
            {
                _logger.LogOutboxServiceError(
                    LogLevel.Error,
                    message.Payload.Subject,
                    StorageProvider.AzureBlob,
                    $"Integrity check failed for file {file.Id}. Expected MD5: {file.HashMd5}, Azure MD5: {azureMd5Base64}"
                );
                await _storedFileService.UpdateCorruptedUploadStatusAsync(fileId.Value, ct);
                
                return;
            }
            
            var payload = new StoredFileUploadedPayload(
                file.FileCategory.Code,
                file.Id,
                file.FileCategory.TenantId,
                file.FileName,
                file.ContentType);
            
            await _storedFileUploadedMessageProducer.PublishAsync(
                payload, 
                ct);
           
        }
        catch (Exception ex)
        {
            _logger.LogOutboxServiceError(
                LogLevel.Error,
                message.Payload.Subject,
                StorageProvider.AzureBlob,
                ex.Message,
                ex
            );
            throw;
        }

    }

}