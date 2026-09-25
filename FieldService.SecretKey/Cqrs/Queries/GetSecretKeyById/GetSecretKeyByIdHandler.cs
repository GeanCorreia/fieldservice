using FieldService.SecretKey.Dtos;
using FieldService.SecretKey.Entities;
using FieldService.SecretKey.Interfaces;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Queries.GetSecretKeyById;


internal sealed class GetSecretKeyByIdHandler<T> : IRequestHandler<GetSecretKeyByIdQuery<T>, T?>
    where T : class, ISecretKeyType
{
    private readonly ISecretKeyService _secretKeyService;
    private readonly ISecretKeyVaultService _secretKeyVaultService;

    public GetSecretKeyByIdHandler(
        ISecretKeyService secretKeyService,
        ISecretKeyVaultService secretKeyVaultService)
    {
        _secretKeyService = secretKeyService ?? throw new ArgumentNullException(nameof(secretKeyService));
        _secretKeyVaultService = secretKeyVaultService ?? throw new ArgumentNullException(nameof(secretKeyVaultService));
    }

    public async Task<T?> Handle(GetSecretKeyByIdQuery<T> request, CancellationToken cancellationToken)
    {
        var secretKey = await _secretKeyService.GetSecretKeyAsync(request.SecretKeyId, cancellationToken);
        if (secretKey is null)
            return null;

        var expectedType = GetExpectedSecretKeyType();
        if (secretKey.Type != expectedType)
            throw new InvalidOperationException(
                $"Secret key '{secretKey.Id}' is of type '{secretKey.Type}' but '{typeof(T).Name}' was requested.");

        var secret = await _secretKeyVaultService.GetSecretKeyAsync<T>(
            secretKey.TenantId,
            secretKey.Reference.Name,
            cancellationToken);

        return secret;

    }

    private static SecretKeyType GetExpectedSecretKeyType()
    {
        return typeof(T) switch
        {
            var type when type == typeof(ConnectionStringSecret) => SecretKeyType.ConnectionString,
            var type when type == typeof(UserPasswordSecret) => SecretKeyType.UserPassword,
            var type when type == typeof(ApiKeySecret) => SecretKeyType.ApiKey,
            var type when type == typeof(OAuthClientSecret) => SecretKeyType.OAuthClientSecret,
            var type when type == typeof(AsymmetricKeyPairSecret) => SecretKeyType.AsymmetricKeyPair,
            var type when type == typeof(CertificateSecret) => SecretKeyType.CertificateSecret,
            var type when type == typeof(WebhookSigningSecret) => SecretKeyType.WebhookSigningSecret,
            var type when type == typeof(StorageCredentialsSecret) => SecretKeyType.StorageCredentials,
            _ => throw new NotSupportedException($"Unsupported secret key type '{typeof(T).Name}'.")
        };
    }
}