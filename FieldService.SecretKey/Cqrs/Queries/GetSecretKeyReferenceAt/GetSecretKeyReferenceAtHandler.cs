using FieldService.SecretKey.Dtos;
using FieldService.SecretKey.Interfaces;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Queries.GetSecretKeyReferenceAt;

public class GetSecretKeyReferenceAtHandler : IRequestHandler<GetSecretKeyReferenceAtQuery, SecretKeyHistoryDto>
{
    private readonly ISecretKeyRepository _secretKeyRepository;

    internal GetSecretKeyReferenceAtHandler(ISecretKeyRepository secretKeyRepository)
    {
        _secretKeyRepository = secretKeyRepository;
    }

    public async Task<SecretKeyHistoryDto> Handle(GetSecretKeyReferenceAtQuery request, CancellationToken cancellationToken)
    {
        var secretKeyReference = await _secretKeyRepository.GetSecretKeyAsync(request.TenantId, request.SecretName, cancellationToken);
        if (secretKeyReference == null)
        {
            throw new KeyNotFoundException($"Secret key reference not found for tenant {request.TenantId} and secret name {request.SecretName} at {request.ReferenceDate}");
        }

        return new SecretKeyHistoryDto(
            AtTime: request.ReferenceDate ?? DateTimeOffset.UtcNow,
            Reference: secretKeyReference.SecretKeyAt(request.ReferenceDate)
        );
        
    }
}
