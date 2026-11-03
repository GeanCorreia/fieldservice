using FieldService.Authorization.Cqrs.Command;
using FieldService.Authorization.Interfaces;
using MediatR;

namespace FieldService.Authorization.Cqrs.Command.CancelUserRevocation;

internal sealed class CancelUserRevocationHandler(
    IUserContextRepository userContextRepository,
    IAuthorizationService authorizationService) : IRequestHandler<CancelUserRevocationCommand>
{
    public async Task Handle(CancelUserRevocationCommand request, CancellationToken cancellationToken)
    {
        var context = await UserAuthorizationCommandHelpers.LoadForTenantOrThrowAsync(
            userContextRepository,
            request.UserId,
            request.TenantId,
            cancellationToken);
        
        if(!context.IsRevoked(DateTimeOffset.UtcNow))
        {
            return;
        }

        var effectiveCreatedBy = UserAuthorizationCommandHelpers.ResolveCreatedBy(request.CreatedBy, request.UserId);
        var effectiveCancelledAt = request.CancelledAt ?? DateTimeOffset.UtcNow;

        context.CancelTenantRevocation(effectiveCancelledAt, effectiveCreatedBy);

        await userContextRepository.SaveUserContext(context, cancellationToken);
        await authorizationService.RemoveUserFromCacheAsync(request.UserId, cancellationToken);
    }
}

