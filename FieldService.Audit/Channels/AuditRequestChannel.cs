using System.Threading.Channels;
using FieldService.Audit.Entities;

namespace FieldService.Audit.Channels;

public class AuditRequestChannel
{
    private readonly Channel<AuditRequest> _channel;

    public AuditRequestChannel()
    {
        var options = new BoundedChannelOptions(capacity: 1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<AuditRequest>(options);
    }
    
    public ValueTask EnqueueAsync(AuditRequest activity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        
        if (_channel.Writer.TryWrite(activity))
        {
            return ValueTask.CompletedTask;
        }
        
        return _channel.Writer.WriteAsync(activity, ct);
    }
    
    public ChannelReader<AuditRequest> Reader => _channel.Reader;
}