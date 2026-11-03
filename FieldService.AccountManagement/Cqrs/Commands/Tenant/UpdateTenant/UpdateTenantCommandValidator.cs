using FluentValidation;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateTenant;

internal sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.UpdatedBy)
            .NotEmpty();

        RuleFor(x => x)
            .Must(HasAtLeastOneValue)
            .WithMessage("At least one property must be provided for update.");
    }

    private static bool HasAtLeastOneValue(UpdateTenantCommand command)
    {
        return !string.IsNullOrWhiteSpace(command.Name);
    }
}

