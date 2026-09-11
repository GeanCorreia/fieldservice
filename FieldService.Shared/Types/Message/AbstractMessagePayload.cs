using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldService.Shared.Message;

public abstract record AbstractMessagePayload<TPayload> : IMessagePayload
    where TPayload : AbstractMessagePayload<TPayload>
{
    protected static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public static TSelf Deserialize<TSelf>(ReadOnlySpan<byte> bytes) 
        where TSelf : class, IMessagePayload
    {
        return (JsonSerializer.Deserialize<TPayload>(bytes, DefaultOptions) as TSelf)
               ?? throw new InvalidOperationException($"Falha ao desserializar o payload do tipo '{typeof(TPayload).Name}'.");
    }
    
    public byte[] ToBytes() 
        => JsonSerializer.SerializeToUtf8Bytes(this, GetType(), DefaultOptions);
}