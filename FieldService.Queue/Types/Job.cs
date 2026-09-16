using System.Text.Json.Serialization;

namespace FieldService.Queue.Types;

[JsonConverter(typeof(JobTypeJsonConverter))]
public readonly record struct JobType
{
    public string Value { get; }

    public JobType(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static JobType From<TJob>() => new(typeof(TJob).Name);
    public static JobType From(Type type) => new(type.Name);

    public static implicit operator string(JobType jobType) => jobType.Value;
    public static implicit operator JobType(string value) => new(value);

    public override string ToString() => Value;
}


public record JobContext
{
    public JobType Type { get; init; }
    public string CorrelationId { get; init; }
    public Guid? TenantId { get; init; }

    [JsonConstructor]
    public JobContext(JobType type, string correlationId, Guid? tenantId = null)
    {
        Type = type;
        CorrelationId = correlationId;
        TenantId = tenantId;
    }
    
    public JobContext(JobType type, Guid? tenantId = null, string? correlationId = null)
    {
        Type = type;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId;
        TenantId = tenantId;
    }
}

public record Job(
    JobContext Context)
{
    public string JobId => Context.Type.Value;
}

public record Job<TPayload>(
    TPayload Payload,
    JobContext Context) : Job(Context)
{
}
