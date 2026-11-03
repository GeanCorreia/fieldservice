using FluentValidation;

namespace FieldService.AccountManagement.Cqrs.Commands.DeactivateJobTitle;

internal sealed class DeactivateJobTitleCommandValidator : AbstractValidator<DeactivateJobTitleCommand>
{
    public DeactivateJobTitleCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.JobTitleId).NotEmpty();
        RuleFor(x => x.UpdatedBy).NotEmpty();
        RuleFor(x => x.TransferJobTitle)
            .NotEmpty()
            .Must((command, replacement) => replacement != command.JobTitleId)
            .When(x => x.TransferJobTitle.HasValue);
    }
}
