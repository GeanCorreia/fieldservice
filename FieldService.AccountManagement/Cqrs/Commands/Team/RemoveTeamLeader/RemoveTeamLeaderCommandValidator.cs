using FluentValidation;

namespace FieldService.AccountManagement.Cqrs.Commands.RemoveTeamLeader;

internal sealed class RemoveTeamLeaderCommandValidator : AbstractValidator<RemoveTeamLeaderCommand>
{
    public RemoveTeamLeaderCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
