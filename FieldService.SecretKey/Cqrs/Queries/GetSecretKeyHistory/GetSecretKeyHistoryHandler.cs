using FieldService.SecretKey.Dtos;
using FieldService.SecretKey.Interfaces;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Queries.GetSecretKeyHistory;

public class GetSecretKeyHistoryHandler : IRequestHandler<GetSecretKeyHistoryQuery, IEnumerable<SecretKeyHistoryDto>>
{
    private readonly ISecretKeyRepository _secretKeyRepository;
    
    internal GetSecretKeyHistoryHandler(ISecretKeyRepository secretKeyRepository)
    {
        _secretKeyRepository = secretKeyRepository ?? throw new ArgumentNullException(nameof(secretKeyRepository));
    }
    
    public async Task<IEnumerable<SecretKeyHistoryDto>> Handle(GetSecretKeyHistoryQuery request, CancellationToken cancellationToken)
    {
        var secret = await _secretKeyRepository.GetSecretKeyAsync(request.TenantId, request.SecretName, cancellationToken);
        if (secret == null)
        {
            throw new KeyNotFoundException($"Secret key '{request.SecretName}' not found for tenant '{request.TenantId}'.");
        }
        
        return secret.History;
    }
}