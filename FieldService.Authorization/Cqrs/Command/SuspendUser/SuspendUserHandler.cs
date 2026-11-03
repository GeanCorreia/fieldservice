using FieldService.Authorization.Cqrs.Command;
using FieldService.Authorization.Interfaces;
using MediatR;

namespace FieldService.Authorization.Cqrs.Command.SuspendUser;

internal sealed class SuspendUserHandler(
    IUserContextRepository userContextRepository,
    IAuthorizationService authorizationService) : IRequestHandler<SuspendUserCommand>
{
    public async Task Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        var context = await UserAuthorizationCommandHelpers.LoadForTenantOrThrowAsync(
            userContextRepository,
            request.UserId,
            request.TenantId,
            cancellationToken);

        var effectiveCreatedBy = UserAuthorizationCommandHelpers.ResolveCreatedBy(request.CreatedBy, request.UserId);
        var effectiveCreatedAt = request.CreatedAt ?? DateTimeOffset.UtcNow;

        var activeSuspensions = context.GetActiveSuspensions(effectiveCreatedAt);
        if (activeSuspensions.Any(suspension => suspension.Source == request.Source))
            return;

        context.Deactivate(
            effectiveCreatedBy,
            request.Source,
            request.StartedAt,
            request.EndedAt,
            effectiveCreatedAt);

        await userContextRepository.SaveUserContext(context, cancellationToken);
        await authorizationService.RemoveUserFromCacheAsync(request.UserId, cancellationToken);
    }
}


