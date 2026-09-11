using FluentValidation;

namespace FieldService.Http.Cqrs.Commands.Login;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    
    public LoginValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
    }
}
