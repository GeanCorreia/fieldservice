using FieldService.Storage.Channels;
using FieldService.Storage.Interfaces;
using FieldService.Storage.Logs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FieldService.Storage.Services;

internal class StoredFileFailedUploadOutboxService : BackgroundService
{
    private readonly ILogger<StoredFileFailedUploadOutboxService> _logger;
    private readonly StoredFileFailedUploadOutboxChannel _failedUploadOutboxChannel;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    public StoredFileFailedUploadOutboxService(
        ILogger<StoredFileFailedUploadOutboxService> logger,
        StoredFileFailedUploadOutboxChannel failedUploadOutboxChannel,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger 
                  ?? throw new ArgumentNullException(nameof(logger));
        _failedUploadOutboxChannel = failedUploadOutboxChannel 
                                     ?? throw new ArgumentNullException(nameof(failedUploadOutboxChannel));
        _serviceScopeFactory = serviceScopeFactory 
                               ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var fileId in _failedUploadOutboxChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var storedFileService = scope.ServiceProvider.GetRequiredService<IStoredFileService>();
                await storedFileService.UpdateFailedUploadStatusAsync(fileId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogFailedUploadOutboxServiceError(
                    LogLevel.Error, 
                    fileId, 
                    ex);
            }
        }
    }
    
   }