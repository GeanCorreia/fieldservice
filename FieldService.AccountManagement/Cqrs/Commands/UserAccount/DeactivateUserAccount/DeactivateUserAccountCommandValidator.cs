using FluentValidation;

namespace FieldService.AccountManagement.Cqrs.Commands.DeactivateUserAccount;

internal sealed class DeactivateUserAccountCommandValidator : AbstractValidator<DeactivateUserAccountCommand>
{
    public DeactivateUserAccountCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
