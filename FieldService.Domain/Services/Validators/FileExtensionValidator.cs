namespace FieldService.Domain.Services.Validators;

using FluentValidation.Results;
using FieldService.Domain.Entities;
using FieldService.Domain.ValueObjects;

public class FileExtensionValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule) 
        => rule.Type == FieldService.Domain.Enums.ValidatorType.FileExtension;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return Array.Empty<ValidationFailure>();

        if (!rule.Parameters.TryGetValue("extensions", out var extensionsObj) || extensionsObj is not string[] extensions)
            return Array.Empty<ValidationFailure>();

        var filePath = value.ToString() ?? "";
        var fileExtension = System.IO.Path.GetExtension(filePath).TrimStart('.');

        if (!extensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
        {
            return new[]
            {
                new ValidationFailure(field.Name, $"{field.Label}: Extensão deve ser: {string.Join(", ", extensions)}")
            };
        }

        return Array.Empty<ValidationFailure>();
    }
}
