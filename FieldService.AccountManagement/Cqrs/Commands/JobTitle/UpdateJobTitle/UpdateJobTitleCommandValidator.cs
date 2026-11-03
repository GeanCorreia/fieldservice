using FluentValidation;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateJobTitle;

internal sealed class UpdateJobTitleCommandValidator : AbstractValidator<UpdateJobTitleCommand>
{
    public UpdateJobTitleCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.JobTitleId)
            .NotEmpty();

        RuleFor(x => x.UpdatedBy)
            .NotEmpty();

        RuleFor(x => x)
            .Must(HasAtLeastOneValue)
            .WithMessage("At least one property must be provided for update.");
    }

    private static bool HasAtLeastOneValue(UpdateJobTitleCommand command)
    {
        return !string.IsNullOrWhiteSpace(command.Title)
            || command.Description is not null;
    }
}
