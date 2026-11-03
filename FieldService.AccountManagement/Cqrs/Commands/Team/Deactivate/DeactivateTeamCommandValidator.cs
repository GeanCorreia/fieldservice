using FluentValidation;

namespace FieldService.AccountManagement.Cqrs.Commands.DeactivateTeam;

internal sealed class DeactivateTeamCommandValidator : AbstractValidator<DeactivateTeamCommand>
{
    public DeactivateTeamCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
