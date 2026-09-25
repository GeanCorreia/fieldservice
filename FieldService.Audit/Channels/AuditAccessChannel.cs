using System.Threading.Channels;
using FiledService.Audit.Entities;

namespace FieldService.Audit.Channels;

public class AuditAccessChannel
{
    private readonly Channel<AuditAccess> _channel;

    public AuditAccessChannel()
    {
        var options = new BoundedChannelOptions(capacity: 1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<AuditAccess>(options);
    }

    public ValueTask EnqueueAsync(AuditAccess auditAccess, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(auditAccess);

        if (_channel.Writer.TryWrite(auditAccess))
            return ValueTask.CompletedTask;

        return _channel.Writer.WriteAsync(auditAccess, ct);
    }

    public ChannelReader<AuditAccess> Reader => _channel.Reader;
}

