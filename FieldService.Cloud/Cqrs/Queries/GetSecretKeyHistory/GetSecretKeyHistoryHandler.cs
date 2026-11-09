using FieldService.Cloud.Dtos;
using FieldService.Cloud.Interfaces;
using MediatR;

namespace FieldService.Cloud.Cqrs.Queries.GetSecretKeyHistory;

internal class GetSecretKeyHistoryHandler : IRequestHandler<GetSecretKeyHistoryQuery, IEnumerable<SecretKeyHistoryDto>>
{
    private readonly ISecretKeyService _secretKeyService;
    private readonly IMediator _mediator;
    
    public GetSecretKeyHistoryHandler(ISecretKeyService secretKeyService, IMediator mediator)
    {
        _secretKeyService = secretKeyService ?? throw new ArgumentNullException(nameof(secretKeyService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }
    
    public async Task<IEnumerable<SecretKeyHistoryDto>> Handle(GetSecretKeyHistoryQuery request, CancellationToken cancellationToken)
    {
        var secret = await _secretKeyService.GetSecretKeyAsync(request.TenantId, request.SecretName, cancellationToken);
        if (secret == null)
        {
            throw new KeyNotFoundException($"Secret key '{request.SecretName}' not found for tenant '{request.TenantId}'.");
        }
        
        return secret.History;
    }
}