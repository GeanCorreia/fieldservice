using System.Threading.Channels;
using FieldService.Broker.Entities;
using FieldService.Broker.Interfaces;

namespace FieldService.Broker.Channels;

public class BrokerMessageChannel
{
    private readonly Channel<BrokerOutbox> _channel;

    public BrokerMessageChannel()
    {
        var options = new BoundedChannelOptions(capacity: 1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false 
        };
        
        _channel = Channel.CreateBounded<BrokerOutbox>(options);
    }
    
    public async ValueTask EnqueueAsync(BrokerOutbox brokerOutbox, CancellationToken ct = default)  
    {
        ArgumentNullException.ThrowIfNull(brokerOutbox);
        await _channel.Writer.WriteAsync(brokerOutbox, ct);
    }

    public async ValueTask WriteAsync(BrokerOutbox brokerOutbox, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(brokerOutbox);
        await EnqueueAsync(brokerOutbox, ct);
    }
    
    public ChannelReader<BrokerOutbox> Reader => _channel.Reader;
}