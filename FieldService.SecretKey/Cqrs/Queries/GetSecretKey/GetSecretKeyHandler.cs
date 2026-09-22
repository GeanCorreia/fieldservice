using FieldService.SecretKey.Interfaces;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Queries.GetSecretKey;

public class GetSecretKeyHandler<T> : IRequestHandler<GetSecretKeyQuery<T>, T> where T : ISecretKeyType
{
    private readonly ISecretKeyVaultService _secretKeyVaultService;
    private readonly ISecretKeyRepository _secretKeyRepository;
    
    internal GetSecretKeyHandler(ISecretKeyVaultService secretKeyVaultService)
    {
        _secretKeyVaultService = secretKeyVaultService ?? throw new ArgumentNullException(nameof(secretKeyVaultService));
    }

    public async Task<T?> Handle(GetSecretKeyQuery<T> request, CancellationToken cancellationToken)
    {
        return  await _secretKeyVaultService.GetSecretKeyAsync<T>(
            request.TenantId, 
            request.SecretName, 
            cancellationToken);
        
        
    }
}