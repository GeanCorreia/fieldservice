namespace FieldService.TemplateService.Services.Validators;

using FluentValidation.Results;
using FieldService.TemplateService.Entities;
using FieldService.Shared.Types;

public class DateRangeValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule) 
        => rule.Type == FieldService.TemplateService.Enums.ValidatorType.DateRange;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return Array.Empty<ValidationFailure>();

        if (!DateTime.TryParse(value.ToString() ?? "", out var dateValue))
            return Array.Empty<ValidationFailure>();

        if (!rule.Parameters.TryGetValue("from", out var fromObj) || !DateTime.TryParse(fromObj?.ToString(), out var from) ||
            !rule.Parameters.TryGetValue("to", out var toObj) || !DateTime.TryParse(toObj?.ToString(), out var to))
            return Array.Empty<ValidationFailure>();

        if (dateValue < from || dateValue > to)
        {
            return new[]
            {
                new ValidationFailure(field.Name, $"{field.Label}: Data deve estar entre {from:dd/MM/yyyy} e {to:dd/MM/yyyy}")
            };
        }

        return Array.Empty<ValidationFailure>();
    }
}
