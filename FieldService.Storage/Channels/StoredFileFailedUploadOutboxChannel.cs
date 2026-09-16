using System.Threading.Channels;
using FieldService.Storage.Entities;

namespace FieldService.Storage.Channels;

public class StoredFileFailedUploadOutboxChannel
{
    private readonly Channel<Guid> _channel;

    public StoredFileFailedUploadOutboxChannel()
    {
        var options = new BoundedChannelOptions(capacity: 1000)
        {
            FullMode = BoundedChannelFullMode.Wait 
        };
        _channel = Channel.CreateBounded<Guid>(options);
    }
    
    public async ValueTask EnqueueAsync(Guid fileId, CancellationToken ct = default)  
    {
        await _channel.Writer.WriteAsync(fileId, ct);
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken ct = default)
    {
        var fileId = await _channel.Reader.ReadAsync(ct);
        return fileId;
    }
    
    public async ValueTask WriteAsync(Guid fileId, CancellationToken ct = default)
    {
        await EnqueueAsync(fileId, ct);
    }
    public ChannelReader<Guid> Reader => _channel.Reader;
}