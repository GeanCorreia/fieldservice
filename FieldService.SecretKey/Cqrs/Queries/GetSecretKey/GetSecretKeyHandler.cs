using FieldService.SecretKey.Interfaces;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Queries.GetSecretKey;

internal class GetSecretKeyHandler<T> : IRequestHandler<GetSecretKeyQuery<T>, T> where T : class, ISecretKeyType
{
    private readonly ISecretKeyVaultService _secretKeyVaultService;
    
    public GetSecretKeyHandler(ISecretKeyVaultService secretKeyVaultService)
    {
        _secretKeyVaultService = secretKeyVaultService ?? throw new ArgumentNullException(nameof(secretKeyVaultService));
    }

    public async Task<T> Handle(GetSecretKeyQuery<T> request, CancellationToken cancellationToken)
    {
        var secret = await _secretKeyVaultService.GetSecretKeyAsync<T>(
            request.TenantId, 
            request.SecretName, 
            cancellationToken);

        return secret ?? throw new KeyNotFoundException(
            $"Secret key '{request.SecretName}' was not found for tenant '{request.TenantId}'.");
    }
}