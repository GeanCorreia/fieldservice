namespace FieldService.Domain.Services.Validators;

using FluentValidation.Results;
using System.Text.RegularExpressions;
using FieldService.Domain.Entities;
using FieldService.Domain.ValueObjects;

public class RegexValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule) 
        => rule.Type == FieldService.Domain.Enums.ValidatorType.Regex;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return Array.Empty<ValidationFailure>();

        if (!rule.Parameters.TryGetValue("pattern", out var patternObj))
            return Array.Empty<ValidationFailure>();

        var pattern = patternObj?.ToString() ?? "";
        try
        {
            if (!Regex.IsMatch(value.ToString() ?? "", pattern))
            {
                return new[]
                {
                    new ValidationFailure(field.Name, $"{field.Label}: Formato inválido")
                };
            }
        }
        catch
        {
            return new[]
            {
                new ValidationFailure(field.Name, $"{field.Label}: Erro ao validar formato")
            };
        }

        return Array.Empty<ValidationFailure>();
    }
}
