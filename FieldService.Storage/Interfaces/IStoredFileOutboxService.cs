namespace FieldService.Storage.Interfaces;

internal interface IStoredFileOutboxService
{
    Task ProcessFailedUploadASync(
        Guid fileId, 
        CancellationToken token);

    Task ProcessCanceledUploadAsync(
        Guid fileId, 
        CancellationToken token);
}

