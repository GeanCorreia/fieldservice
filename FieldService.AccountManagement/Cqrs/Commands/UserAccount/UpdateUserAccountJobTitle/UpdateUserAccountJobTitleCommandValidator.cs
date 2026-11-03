using FluentValidation;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateUserAccountJobTitle;

internal sealed class UpdateUserAccountJobTitleCommandValidator : AbstractValidator<UpdateUserAccountJobTitleCommand>
{
    public UpdateUserAccountJobTitleCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.JobTitleId).NotEmpty();
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
