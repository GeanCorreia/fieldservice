using FieldService.Http.Cqrs.Queries.GetUser;
using FieldService.SecretKey.Dtos;
using FieldService.SecretKey.Interfaces;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Queries.GetSecretKeyHistory;

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
        var user = await _mediator.Send(new GetUserQuery(request.UserId), cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException($"User not found.");
        }
        
        var userTenants = user.Tenants.Select(t => t.TenantId).ToList();
        
        if(!userTenants.Contains(request.TenantId) )
        {
            throw new UnauthorizedAccessException($"User does not have access to tenant '{request.TenantId}'.");
        }
        
        var secret = await _secretKeyService.GetSecretKeyAsync(request.TenantId, request.SecretName, cancellationToken);
        if (secret == null)
        {
            throw new KeyNotFoundException($"Secret key '{request.SecretName}' not found for tenant '{request.TenantId}'.");
        }
        
        return secret.History;
    }
}