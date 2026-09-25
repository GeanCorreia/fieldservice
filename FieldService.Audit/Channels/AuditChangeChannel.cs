using System.Threading.Channels;
using FiledService.Audit.Entities;

namespace FieldService.Audit.Channels;

public class AuditChangeChannel
{
    private readonly Channel<AuditChange> _channel;

    public AuditChangeChannel()
    {
        var options = new BoundedChannelOptions(capacity: 1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<AuditChange>(options);
    }

    public ValueTask EnqueueAsync(AuditChange auditChange, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(auditChange);

        if (_channel.Writer.TryWrite(auditChange))
            return ValueTask.CompletedTask;

        return _channel.Writer.WriteAsync(auditChange, ct);
    }

    public ChannelReader<AuditChange> Reader => _channel.Reader;
}

