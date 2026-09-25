using FieldService.SecretKey.Interfaces;

namespace FieldService.SecretKey.Entities;

public enum SecretKeyType
{
    ConnectionString = 1,
    UserPassword = 2,
    ApiKey = 3,
    OAuthClientSecret = 4,
    AsymmetricKeyPair = 5,
    CertificateSecret = 6,
    WebhookSigningSecret = 7,
    StorageCredentials = 8
}

public enum ApiKeyLocation
{
    Header = 1,
    QueryString = 2
}

public enum HashAlgorithmType
{
    HmacSha256 = 1,
    HmacSha512 = 2,
    RsaSha256 = 3
}

public record ConnectionStringSecret(
    string ConnectionString
) : ISecretKeyType
{
    public static SecretKeyType Type => SecretKeyType.ConnectionString;
};

public record UserPasswordSecret(
    string UserName, 
    string Password
) : ISecretKeyType{
    public static SecretKeyType Type => SecretKeyType.UserPassword;
};

public record ApiKeySecret(
    string ApiKey, 
    ApiKeyLocation Location,
    string HeaderName
) : ISecretKeyType{
    public static SecretKeyType Type => SecretKeyType.ApiKey;
};

public record OAuthClientSecret(
    string ClientId, 
    string ClientSecret, 
    TimeSpan? ExpiresIn = null
) : ISecretKeyType{
    public static SecretKeyType Type => SecretKeyType.OAuthClientSecret;
};

public record AsymmetricKeyPairSecret(
    string PublicKey, 
    string PrivateKey, 
    string? Passphrase = null
) : ISecretKeyType{
    public static SecretKeyType Type => SecretKeyType.AsymmetricKeyPair;
};

public record CertificateSecret(
    string CertificateBase64, 
    string? Password = null
) : ISecretKeyType{
    public static SecretKeyType Type => SecretKeyType.CertificateSecret;
};

public record WebhookSigningSecret(
    string SecretKey, 
    HashAlgorithmType? Algorithm = null
) : ISecretKeyType{
    public static SecretKeyType Type => SecretKeyType.WebhookSigningSecret;
};

public record StorageCredentialsSecret(
    string AccessKeyId, 
    string SecretAccessKey, 
    string? Region = null
) : ISecretKeyType{
    public static SecretKeyType Type => SecretKeyType.StorageCredentials;
};