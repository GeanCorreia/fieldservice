namespace FieldService.Domain.Services.Validators;

using FluentValidation.Results;
using FieldService.Domain.Entities;
using FieldService.Domain.ValueObjects;

public class MinLengthValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule) 
        => rule.Type == FieldService.Domain.Enums.ValidatorType.MinLength;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return Array.Empty<ValidationFailure>();

        if (!rule.Parameters.TryGetValue("value", out var minObj) || !int.TryParse(minObj?.ToString(), out var min))
            return Array.Empty<ValidationFailure>();

        var strValue = value.ToString() ?? "";
        if (strValue.Length < min)
        {
            return new[]
            {
                new ValidationFailure(field.Name, $"{field.Label}: Mínimo de {min} caracteres")
            };
        }

        return Array.Empty<ValidationFailure>();
    }
}
