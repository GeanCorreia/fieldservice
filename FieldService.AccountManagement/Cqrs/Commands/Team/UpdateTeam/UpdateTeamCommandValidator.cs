using FluentValidation;

namespace FieldService.AccountManagement.Cqrs.Commands.UpdateTeam;

internal sealed class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.TeamId)
            .NotEmpty();

        RuleFor(x => x.UpdatedBy)
            .NotEmpty();

        RuleFor(x => x)
            .Must(HasAtLeastOneValue)
            .WithMessage("At least one property must be provided for update.");
    }

    private static bool HasAtLeastOneValue(UpdateTeamCommand command)
    {
        return !string.IsNullOrWhiteSpace(command.Name)
            || command.Description is not null
            || command.LeaderId.HasValue;
    }
}
