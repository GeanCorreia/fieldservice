using FieldService.Authorization.Cqrs.Command;
using FieldService.Authorization.Events;
using FieldService.Authorization.Interfaces;
using MediatR;

namespace FieldService.Authorization.Cqrs.Command.ActivateUser;

internal sealed class ActivateUserHandler(
    IUserContextRepository userContextRepository,
    IAuthorizationService authorizationService) : IRequestHandler<ActivateUserCommand>
{
    public async Task Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var context = await UserAuthorizationCommandHelpers.LoadForTenantOrThrowAsync(
            userContextRepository,
            request.UserId,
            request.TenantId,
            cancellationToken);

        var effectiveActivatedAt = request.ActivatedAt ?? DateTimeOffset.UtcNow;
        var effectiveCreatedBy = UserAuthorizationCommandHelpers.ResolveCreatedBy(request.CreatedBy, request.UserId);

        var activeSuspensions = context.GetActiveSuspensions(effectiveActivatedAt);
        if (!activeSuspensions.Any(suspension => suspension.Source == request.Source))
            return;

        var sourceSuspensionEvents = context.GetSuspensionEvents()
            .Where(e =>
                e.Type == SuspensionEventType.Created &&
                e.Source == request.Source &&
                e.CreatedAt <= effectiveActivatedAt &&
                (!e.EndedAt.HasValue || e.EndedAt.Value >= effectiveActivatedAt) &&
                context.GetSuspensionEvents().All(cancel =>
                    cancel.Type != SuspensionEventType.Cancelled ||
                    cancel.OriginalSuspensionEventId != e.Id))
            .ToList();

        foreach (var suspensionEvent in sourceSuspensionEvents)
        {
            context.CancelSuspension(
                suspensionEvent.Id,
                effectiveCreatedBy,
                request.Source,
                effectiveActivatedAt);
        }

        await userContextRepository.SaveUserContext(context, cancellationToken);
        await authorizationService.RemoveUserFromCacheAsync(request.UserId, cancellationToken);
    }
}

