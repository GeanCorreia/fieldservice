namespace FieldService.Cache.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionStringName { get; init; } = "Redis";
    public string InstanceName { get; init; } = "FieldService:";
    public string Host { get; init; } = String.Empty;
    public string Port { get; init; } = "6379";
    public int Database { get; init; } = 0;
    public List<string>? Endpoints { get; init; }
    public string? Password { get; init; }
    public bool UseAzureIdentity { get; init; }
    public string? ManagedIdentityClientId { get; init; }
    public bool Ssl { get; init; }
    public bool AbortOnConnectFail { get; init; } = false;
    public int ConnectTimeoutMs { get; init; } = 5000;
    public int SyncTimeoutMs { get; init; } = 5000;
}