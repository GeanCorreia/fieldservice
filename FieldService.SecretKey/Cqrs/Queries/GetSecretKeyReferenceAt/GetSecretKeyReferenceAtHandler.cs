using FieldService.Http.Cqrs.Queries.GetUser;
using FieldService.SecretKey.Dtos;
using FieldService.SecretKey.Interfaces;
using MediatR;

namespace FieldService.SecretKey.Cqrs.Queries.GetSecretKeyReferenceAt;

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
