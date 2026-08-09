using FluentValidation;

namespace FieldService.Authentication.Cqrs.Commands.Login;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    
    public LoginValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
    }
}
