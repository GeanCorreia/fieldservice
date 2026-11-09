using FieldService.Cloud.Dtos;
using FieldService.Cloud.Interfaces;
using MediatR;

namespace FieldService.Cloud.Cqrs.Queries.GetSecretKeyReferenceAt;

internal class GetSecretKeyReferenceAtHandler : IRequestHandler<GetSecretKeyReferenceAtQuery, SecretKeyHistoryDto>
{
    private readonly ISecretKeyService _secretKeyService;
    private readonly IMediator _mediator;

    public GetSecretKeyReferenceAtHandler(ISecretKeyService secretKeyService, IMediator mediator)
    {
        _secretKeyService = secretKeyService ?? throw new ArgumentNullException(nameof(secretKeyService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task<SecretKeyHistoryDto> Handle(GetSecretKeyReferenceAtQuery request, CancellationToken cancellationToken)
    {
        var secretKeyReference = await _secretKeyService.GetSecretKeyAsync(request.TenantId, request.SecretName, cancellationToken);
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
