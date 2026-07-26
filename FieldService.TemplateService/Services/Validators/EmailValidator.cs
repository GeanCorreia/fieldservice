namespace FieldService.TemplateService.Services.Validators;

using FluentValidation.Results;
using FieldService.TemplateService.Entities;
using FieldService.Shared.Types;

public class EmailValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule) 
        => rule.Type == FieldService.TemplateService.Enums.ValidatorType.Email;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return Array.Empty<ValidationFailure>();

        try
        {
            var email = new System.Net.Mail.MailAddress(value.ToString() ?? "");
            return Array.Empty<ValidationFailure>();
        }
        catch
        {
            return new[]
            {
                new ValidationFailure(field.Name, $"{field.Label}: Email inválido")
            };
        }
    }
}
