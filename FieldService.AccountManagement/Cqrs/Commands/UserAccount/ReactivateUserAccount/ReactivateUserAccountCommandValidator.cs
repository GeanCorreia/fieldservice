using FluentValidation;

namespace FieldService.AccountManagement.Cqrs.Commands.ReactivateUserAccount;

internal sealed class ReactivateUserAccountCommandValidator : AbstractValidator<ReactivateUserAccountCommand>
{
    public ReactivateUserAccountCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
