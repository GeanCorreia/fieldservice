namespace FieldService.Domain.Services.Validators;

using FluentValidation.Results;
using FieldService.Domain.Entities;
using FieldService.Domain.ValueObjects;

public class MinValueValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule) 
        => rule.Type == FieldService.Domain.Enums.ValidatorType.Min;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return Array.Empty<ValidationFailure>();

        if (!rule.Parameters.TryGetValue("value", out var minObj) || 
            !decimal.TryParse(minObj?.ToString(), out var min) ||
            !decimal.TryParse(value.ToString() ?? "", out var numValue))
            return Array.Empty<ValidationFailure>();

        if (numValue < min)
        {
            return new[]
            {
                new ValidationFailure(field.Name, $"{field.Label}: Mínimo de {min}")
            };
        }

        return Array.Empty<ValidationFailure>();
    }
}
