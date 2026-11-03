using FluentValidation;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateUser;

internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.UpdatedBy)
            .NotEmpty();

        RuleFor(x => x)
            .Must(HasAtLeastOneValue)
            .WithMessage("At least one property must be provided for update.");
    }

    private static bool HasAtLeastOneValue(UpdateUserCommand command)
    {
        return !string.IsNullOrWhiteSpace(command.FullName)
            || !string.IsNullOrWhiteSpace(command.DisplayName)
            || command.Email is not null
            || command.Phone is not null
            || command.JobTitleId.HasValue;
    }
}

