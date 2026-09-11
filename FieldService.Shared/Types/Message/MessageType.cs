using FieldService.Shared.Types;

namespace FieldService.Shared.Message;

using System.Text.Json.Serialization;



public record MessageType
{
    public string Name { get; init; }
    
    public SchemaVersion Version { get; init; }
    
    
    public MessageType(string source, string subject, string action, SchemaVersion version)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));
        }
        
        if(string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Subject cannot be null or whitespace.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action cannot be null or whitespace.", nameof(action));
        }

        if (version is null)
        {
            throw new ArgumentNullException(nameof(version), "Version cannot be null.");
        }
        
        Name = $"{source}.{subject}.{action}";
        Version = version;
    }
    
    [JsonConstructor]
    public MessageType(string name, SchemaVersion version)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name), "Name cannot be null.");
        Version = version ?? throw new ArgumentNullException(nameof(version), "Version cannot be null.");
    }
};