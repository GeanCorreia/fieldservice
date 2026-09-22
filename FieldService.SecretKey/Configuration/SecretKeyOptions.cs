namespace FieldService.SecretKey.Configuration;

public sealed class SecretKeyOptions
{
    public const string SectionName = "SecretKey";

    public string KeyVaultName { get; set; } = string.Empty;
    public string KeyVaultEndpoint { get; set; } = string.Empty;

    public int CacheExpirationInHours { get; set; } = 12;
    public int EvictionLockDurationInMinutes { get; set; } = 1;
    public int KeyVaultNetworkTimeout { get; set; } = 10;
    public int KeyVaultMaxRetries { get; set; } = 3;
}