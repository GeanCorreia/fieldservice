namespace FieldService.Domain.Services.Validators;

using FluentValidation.Results;
using System.Text.RegularExpressions;
using FieldService.Domain.Entities;
using FieldService.Domain.ValueObjects;

public class PhoneValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule) 
        => rule.Type == FieldService.Domain.Enums.ValidatorType.Phone;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return Array.Empty<ValidationFailure>();

        var phoneRegex = @"^\+?[1-9]\d{1,14}$";
        if (!Regex.IsMatch(value.ToString() ?? "", phoneRegex))
        {
            return new[]
            {
                new ValidationFailure(field.Name, $"{field.Label}: Telefone inválido")
            };
        }

        return Array.Empty<ValidationFailure>();
    }
}
