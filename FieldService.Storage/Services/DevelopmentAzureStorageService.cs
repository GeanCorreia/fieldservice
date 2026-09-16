using FieldService.Storage.Broker;
using FieldService.Storage.Broker.DevelopmentAzureUploadBrokerMessage;
using FieldService.Storage.Configuration;
using FieldService.Storage.Entities;
using FieldService.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldService.Storage.Services;

internal class DevelopmentAzureStorageService : AzureStorageService
{
    private readonly ILogger<DevelopmentAzureStorageService> _logger;
    private readonly string _containerName;
    private readonly string _blobEndpointBaseUrl;
    private readonly IServiceProvider _serviceProvider;
    

    public DevelopmentAzureStorageService(
        IOptions<StorageOptions> options,
        IServiceProvider serviceProvider,
        ILogger<DevelopmentAzureStorageService> logger) : base(options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
        _containerName = options.Value.AzureBlob.ContainerName;
        _blobEndpointBaseUrl = ResolveBlobEndpointBaseUrl(options.Value.AzureBlob.ConnectionString);
    }

    

    public override async Task UploadStreamAsync(
        StoredFile file,
        Stream content,
        CancellationToken ct = default)
    {

        ArgumentNullException.ThrowIfNull(file);

        await base.UploadStreamAsync(file, content, ct);
        
        var payload = CreateEventGridBlobCreatedPayload(
            file.StoragePath,
            file.ContentType.ToString(),
            content.Length,
            _containerName,
            _blobEndpointBaseUrl);

        try
        {
            // DevelopmentAzureStorageSuccessUploadMessageProducer is scoped; resolve it inside a scope per operation.
            using var scope = _serviceProvider.CreateScope();
            var eventProducer = scope.ServiceProvider
                .GetRequiredService<DevelopmentAzureStorageSuccessUploadMessageProducer>();

            await eventProducer.PublishAsync(payload, ct);
            _logger.LogInformation("[DevStorage] Evento EventGrid simulado e publicado com sucesso para: {StoragePath}", file.StoragePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DevStorage] Falha ao publicar o evento simulado do EventGrid para: {StoragePath}", file.StoragePath);
        }
    }
    
    public override async Task<string> GeneratePresignedUploadUrlAsync(
        StoredFile file,
        TimeSpan expiry, 
        CancellationToken ct = default)
    {
     
        ArgumentNullException.ThrowIfNull(file);

        var url = await base.GeneratePresignedUploadUrlAsync(file, expiry, ct);
        var fileId = ExtractFileIdFromStoragePath(file.StoragePath);
        var tenantId = ExtractTenantIdFromStoragePath(file.StoragePath);
        var jobPayload = new EmulatorEventGridPresignedUrlJobPayload(TenantId: tenantId, FileId: fileId);
        var timeSpan = TimeSpan.FromMinutes(1);

        using var scope = _serviceProvider.CreateScope();
        var eventGridJobProducer = scope.ServiceProvider
            .GetRequiredService<DevelopmentAzureEventGridJobProducer>();
        eventGridJobProducer.PublishDelayed(jobPayload, timeSpan);
        return url;
    }
    
    internal static AzureEventGridBlobCreatedPayload CreateEventGridBlobCreatedPayload(
        string storagePath, 
        string contentType, 
        long contentLength,
        string containerName,
        string blobEndpointBaseUrl = "http://127.0.0.1:10000")
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("Storage path não pode ser nulo ou vazio.", nameof(storagePath));
        }
    
        const string accountName = "devstoreaccount1";
        var blobEndpoint = blobEndpointBaseUrl;
        
        var blobUrl = $"{blobEndpoint}/{accountName}/{containerName}/{storagePath}";
        var subject = $"/blobServices/default/containers/{containerName}/blobs/{storagePath}";

        var diagnostics = new EventGridStorageDiagnostics(
            BatchId: Guid.NewGuid().ToString()
        );

        var data = new EventGridBlobData(
            Api: "PutBlob",
            ClientRequestId: Guid.NewGuid().ToString(),
            RequestId: Guid.NewGuid().ToString(),
            ETag: $"\"{Guid.NewGuid():N}\"",
            ContentType: contentType,
            ContentLength: contentLength,
            BlobType: "BlockBlob",
            Url: blobUrl,
            Sequencer: DateTime.UtcNow.Ticks.ToString("X16"), 
            StorageDiagnostics: diagnostics
        );

        return new AzureEventGridBlobCreatedPayload(
            Topic: $"/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/local/providers/Microsoft.Storage/storageAccounts/{accountName}",
            Subject: subject,
            EventType: "Microsoft.Storage.BlobCreated",
            Id: Guid.NewGuid().ToString(),
            Data: data,
            DataVersion: "1.0",
            MetadataVersion: "1",
            EventTime: DateTimeOffset.UtcNow
        );
    }
    
    
    

    private Guid ExtractFileIdFromStoragePath(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("Storage path cannot be null or empty.", nameof(storagePath));
        }
        
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(storagePath);

        if (!Guid.TryParse(fileNameWithoutExtension, out var fileId))
        {
            throw new ArgumentException($"Invalid storage path format: {storagePath}. Expected a valid GUID filename.");
        }

        return fileId;
    }
    
    private Guid ExtractTenantIdFromStoragePath(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("Storage path cannot be null or empty.", nameof(storagePath));
        }

        var segments = storagePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        var tenantIndex = Array.IndexOf(segments, "tenants");
    
        if (tenantIndex == -1 || tenantIndex + 1 >= segments.Length || !Guid.TryParse(segments[tenantIndex + 1], out var tenantId))
        {
            throw new ArgumentException($"Invalid storage path format: {storagePath}. Expected 'tenants/<tenantId>/...' where <tenantId> is a valid GUID.");
        }

        return tenantId;
    }

    internal static string ResolveBlobEndpointBaseUrl(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "http://127.0.0.1:10000";

        var blobEndpointToken = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(part => part.StartsWith("BlobEndpoint=", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(blobEndpointToken))
            return "http://127.0.0.1:10000";

        var blobEndpoint = blobEndpointToken["BlobEndpoint=".Length..].TrimEnd('/');

        return string.IsNullOrWhiteSpace(blobEndpoint)
            ? "http://127.0.0.1:10000"
            : blobEndpoint;
    }
            
            
}