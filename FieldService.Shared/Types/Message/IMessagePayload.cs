using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldService.Shared.Message;

public interface IMessagePayload
{
    protected static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
}