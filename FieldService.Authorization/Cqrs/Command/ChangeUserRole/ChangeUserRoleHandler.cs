using FieldService.Authorization.Cqrs.Command;
using FieldService.Authorization.Interfaces;
using MediatR;

namespace FieldService.Authorization.Cqrs.Command.ChangeUserRole;

internal sealed class ChangeUserRoleHandler(
    IUserContextRepository userContextRepository,
    IAuthorizationService authorizationService) : IRequestHandler<ChangeUserRoleCommand>
{
    public async Task Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var context = await UserAuthorizationCommandHelpers.LoadForTenantOrThrowAsync(
            userContextRepository,
            request.UserId,
            request.TenantId,
            cancellationToken);
        
        if(context.Role == request.Role)
            return;

        var effectiveCreatedBy = UserAuthorizationCommandHelpers.ResolveCreatedBy(request.CreatedBy, request.UserId);
        var effectiveChangedAt = request.ChangedAt ?? DateTimeOffset.UtcNow;

        context.AddRole(request.Role, effectiveChangedAt, effectiveCreatedBy);

        await userContextRepository.SaveUserContext(context, cancellationToken);
        await authorizationService.RemoveUserFromCacheAsync(request.UserId, cancellationToken);
    }
}

