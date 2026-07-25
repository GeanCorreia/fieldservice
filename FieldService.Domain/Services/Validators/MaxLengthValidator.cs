namespace FieldService.Domain.Services.Validators;

using FluentValidation.Results;
using FieldService.Domain.Entities;
using FieldService.Domain.ValueObjects;

public class MaxLengthValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule) 
        => rule.Type == FieldService.Domain.Enums.ValidatorType.MaxLength;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return Array.Empty<ValidationFailure>();

        if (!rule.Parameters.TryGetValue("value", out var maxObj) || !int.TryParse(maxObj?.ToString(), out var max))
            return Array.Empty<ValidationFailure>();

        var strValue = value.ToString() ?? "";
        if (strValue.Length > max)
        {
            return new[]
            {
                new ValidationFailure(field.Name, $"{field.Label}: Máximo de {max} caracteres")
            };
        }

        return Array.Empty<ValidationFailure>();
    }
}
