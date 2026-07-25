namespace FieldService.Domain.Services.Validators;

using FluentValidation.Results;
using FieldService.Domain.Entities;
using FieldService.Domain.ValueObjects;

public class DecimalPlacesValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule) 
        => rule.Type == FieldService.Domain.Enums.ValidatorType.Decimal;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return Array.Empty<ValidationFailure>();

        if (!rule.Parameters.TryGetValue("places", out var placesObj) || !int.TryParse(placesObj?.ToString(), out var places))
            return Array.Empty<ValidationFailure>();

        if (!decimal.TryParse(value.ToString() ?? "", out var decimalValue))
            return Array.Empty<ValidationFailure>();

        var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(decimalValue)[3])[2];
        if (decimalPlaces > places)
        {
            return new[]
            {
                new ValidationFailure(field.Name, $"{field.Label}: Máximo de {places} casas decimais")
            };
        }

        return Array.Empty<ValidationFailure>();
    }
}
