using FieldService.Storage.Channels;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Services;

internal class StoredFileCanceledUploadOutboxService : BackgroundService
{
    private readonly ILogger<StoredFileCanceledUploadOutboxService> _logger;
    private readonly StoredFileCanceledUploadOutboxChannel _canceledUploadOutboxChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public StoredFileCanceledUploadOutboxService(
        ILogger<StoredFileCanceledUploadOutboxService> logger,
        StoredFileCanceledUploadOutboxChannel canceledUploadOutboxChannel,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _canceledUploadOutboxChannel = canceledUploadOutboxChannel ?? throw new ArgumentNullException(nameof(canceledUploadOutboxChannel));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       await foreach( var fileId in _canceledUploadOutboxChannel.Reader.ReadAllAsync(stoppingToken))
       {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var storedFileService = scope.ServiceProvider.GetRequiredService<IStoredFileService>();
                await storedFileService.UpdateCanceledUploadStatusAsync(fileId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCanceledUploadOutboxServiceError(
                    LogLevel.Error, 
                    fileId, 
                    ex);
            }
       }
    }
    
   
}