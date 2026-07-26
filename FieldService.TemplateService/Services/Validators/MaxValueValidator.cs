namespace FieldService.TemplateService.Services.Validators;

using FluentValidation.Results;
using FieldService.TemplateService.Entities;
using FieldService.Shared.Types;

public class MaxValueValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule) 
        => rule.Type == FieldService.TemplateService.Enums.ValidatorType.Max;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return Array.Empty<ValidationFailure>();

        if (!rule.Parameters.TryGetValue("value", out var maxObj) || 
            !decimal.TryParse(maxObj?.ToString(), out var max) ||
            !decimal.TryParse(value.ToString() ?? "", out var numValue))
            return Array.Empty<ValidationFailure>();

        if (numValue > max)
        {
            return new[]
            {
                new ValidationFailure(field.Name, $"{field.Label}: Máximo de {max}")
            };
        }

        return Array.Empty<ValidationFailure>();
    }
}
