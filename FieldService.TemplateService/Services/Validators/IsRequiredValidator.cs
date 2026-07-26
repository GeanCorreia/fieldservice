namespace FieldService.TemplateService.Services.Validators;

using FluentValidation.Results;
using FieldService.TemplateService.Entities;
using FieldService.TemplateService.Enums;
using FieldService.Shared.Types;

public class IsRequiredValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule)
        => rule.Type == ValidatorType.Required;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new[]
            {
                new ValidationFailure(field.Name, $"{field.Label} é obrigatório")
            };
        }

        return Array.Empty<ValidationFailure>();
    }
}
