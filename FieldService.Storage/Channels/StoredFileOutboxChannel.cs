using System.Threading.Channels;
using FieldService.Storage.Entities;

namespace FieldService.Storage.Channels;

public class StoredFileOutboxChannel
{
    private readonly Channel<StoredFile> _channel;

    public StoredFileOutboxChannel()
    {
        var options = new BoundedChannelOptions(capacity: 1000)
        {
            FullMode = BoundedChannelFullMode.Wait 
        };
        _channel = Channel.CreateBounded<StoredFile>(options);
    }
    
    public async ValueTask EnqueueAsync(StoredFile storedFile, CancellationToken ct = default)  
    {
        ArgumentNullException.ThrowIfNull(storedFile);
        await _channel.Writer.WriteAsync(storedFile, ct);
    }

    public async ValueTask<StoredFile> DequeueAsync(CancellationToken ct = default)
    {
        var storedFile = await _channel.Reader.ReadAsync(ct);
        return storedFile;
    }
    
    public async ValueTask WriteAsync(StoredFile storedFile, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storedFile);
        await EnqueueAsync(storedFile, ct);
    }
    public ChannelReader<StoredFile> Reader => _channel.Reader;
}