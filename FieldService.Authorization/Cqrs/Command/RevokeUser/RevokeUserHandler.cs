using FieldService.Authorization.Cqrs.Command;
using FieldService.Authorization.Interfaces;
using MediatR;

namespace FieldService.Authorization.Cqrs.Command.RevokeUser;

internal sealed class RevokeUserHandler(
    IUserContextRepository userContextRepository,
    IAuthorizationService authorizationService) : IRequestHandler<RevokeUserCommand>
{
    public async Task Handle(RevokeUserCommand request, CancellationToken cancellationToken)
    {
        var context = await UserAuthorizationCommandHelpers.LoadForTenantOrThrowAsync(
            userContextRepository,
            request.UserId,
            request.TenantId,
            cancellationToken);

        if (context.IsRevoked(DateTimeOffset.UtcNow))
        {
            return;
        }

        var effectiveCreatedBy = UserAuthorizationCommandHelpers.ResolveCreatedBy(request.CreatedBy, request.UserId);
        var effectiveRevokedAt = request.RevokedAt ?? DateTimeOffset.UtcNow;

        context.RevokeTenantMembership(effectiveRevokedAt, effectiveCreatedBy);

        await userContextRepository.SaveUserContext(context, cancellationToken);
        await authorizationService.RemoveUserFromCacheAsync(request.UserId, cancellationToken);
    }
}

