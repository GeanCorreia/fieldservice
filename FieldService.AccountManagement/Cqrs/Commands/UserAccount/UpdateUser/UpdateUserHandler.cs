using FieldService.AccountManagement.Interfaces;
using MediatR;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateUser;

internal sealed class UpdateUserHandler(
    IAccountManagementRepository repository,
    IAccountManagementService cacheService) : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.GetUserByIdAsync(request.TenantId, request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{request.UserId}' was not found for tenant '{request.TenantId}'.");

        var eventsCountBeforeUpdate = user.Events.Count;

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.UpdateFullName(request.FullName.Trim(), request.UpdatedBy);

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.UpdateDisplayName(request.DisplayName.Trim(), request.UpdatedBy);

        if (request.Email is not null)
        {
            if (await repository.HasUserAsync(request.TenantId, request.Email, request.UserId, cancellationToken))
                throw new InvalidOperationException($"A user with email '{request.Email}' already exists.");

            user.UpdateEmail(request.Email, request.UpdatedBy);
        }

        if (request.Phone is not null)
            user.UpdatePhone(request.Phone, request.UpdatedBy);

        if (request.JobTitleId.HasValue && request.JobTitleId.Value != user.JobTitle?.Id)
        {
            throw new InvalidOperationException(
                "Changing JobTitleId is not supported in UpdateUser. Use a dedicated reassignment flow.");
        }

        if (eventsCountBeforeUpdate == user.Events.Count)
            return;

        await repository.SaveAsync(user, cancellationToken);
        await cacheService.RemoveCache(request.TenantId, entityId: null, cancellationToken);
    }
}
