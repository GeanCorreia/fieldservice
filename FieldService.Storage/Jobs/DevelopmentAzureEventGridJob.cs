using FieldService.Queue.Interfaces;
using FieldService.Queue.Producers;
using FieldService.Queue.Types;
using FieldService.Storage.Broker.DevelopmentAzureUploadBrokerMessage;
using FieldService.Storage.Configuration;
using FieldService.Storage.Entities;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Services;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FieldService.Storage.Jobs;

internal record EmulatorEventGridPresignedUrlJobPayload(Guid TenantId, Guid FileId);

internal sealed record DevelopmentAzureEventGridJob : Job<EmulatorEventGridPresignedUrlJobPayload>
{
    public static readonly JobType JobType = "emulator-event-grid-presigned-url";
    
    
    internal DevelopmentAzureEventGridJob(EmulatorEventGridPresignedUrlJobPayload payload)
        : base(payload, new JobContext(JobType, tenantId: payload.TenantId))
    {}
    
}

internal class DevelopmentAzureEventGridJobHandler : IQueueConsumer<EmulatorEventGridPresignedUrlJobPayload>
{
    private readonly IStorageProviderFactory _storageProviderFactory;
    private readonly IStoredFileRepository _storedFileRepository;
    private readonly string _containerName;
    private readonly string _blobEndpointBaseUrl;
    private readonly DevelopmentAzureStorageSuccessUploadMessageProducer _producer;
    
    public DevelopmentAzureEventGridJobHandler(
        IOptions<StorageOptions> options,
        IStorageProviderFactory storageProviderFactory,
        IStoredFileRepository storedFileRepository,
        DevelopmentAzureStorageSuccessUploadMessageProducer producer)
    {
        ArgumentNullException.ThrowIfNull(options);
        _storageProviderFactory = storageProviderFactory ?? throw new ArgumentNullException(nameof(storageProviderFactory));
        _storedFileRepository = storedFileRepository ?? throw new ArgumentNullException(nameof(storedFileRepository));
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _containerName = options.Value.AzureBlob.ContainerName;
        _blobEndpointBaseUrl = DevelopmentAzureStorageService
            .ResolveBlobEndpointBaseUrl(options.Value.AzureBlob.ConnectionString);
    }
    public async Task ExecuteAsync(
        Job<EmulatorEventGridPresignedUrlJobPayload> job, CancellationToken ct = default)
    {
        var file = await _storedFileRepository.GetByIdAsync(job.Payload.FileId, ct);
        
        if(file == null || file.Status != StorageStatus.Pending)
            return;
        
        var provider = _storageProviderFactory.GetProvider(file.Provider);
        
        var hadUploaded = await provider.HasFilesAsync(
            new List<StoredFile> { file },
            ct);

        var status = hadUploaded
            .FirstOrDefault(f => f.File.Id == file.Id).Exists;
        
        if(!status)
            return;
        
        var payload  = DevelopmentAzureStorageService.CreateEventGridBlobCreatedPayload(
            file.StoragePath, 
            file.ContentType.ToString(), 
            file.Size,
            _containerName,
            _blobEndpointBaseUrl);
        
        await _producer.PublishAsync(payload, ct);

    }
    
}


internal class DevelopmentAzureEventGridJobProducer 
    : AbstractPublishDelayedProducer<DevelopmentAzureEventGridJobHandler, EmulatorEventGridPresignedUrlJobPayload>
{
    public DevelopmentAzureEventGridJobProducer(IBackgroundJobClient backgroundJobClient)
        : base(backgroundJobClient)
    {
    }

    public string PublishDelayed(EmulatorEventGridPresignedUrlJobPayload payload, TimeSpan delay)
    {
        var job = new DevelopmentAzureEventGridJob(payload);
        return PublishDelayed(job, delay);
    }
}