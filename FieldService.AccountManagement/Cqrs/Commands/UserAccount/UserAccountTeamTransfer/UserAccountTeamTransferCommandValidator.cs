using FluentValidation;

namespace FieldService.AccountManagement.Cqrs.Commands.UserAccountTeamTransfer;

internal sealed class UserAccountTeamTransferCommandValidator : AbstractValidator<UserAccountTeamTransferCommand>
{
    public UserAccountTeamTransferCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
