namespace FieldService.TemplateService.Services.Validators;

using FluentValidation.Results;
using FieldService.TemplateService.Entities;
using FieldService.Shared.Types;

public class FileSizeValidator : IFieldValidator
{
    public bool CanValidate(ValidationRule rule) 
        => rule.Type == FieldService.TemplateService.Enums.ValidatorType.FileSize;

    public ValidationFailure[] Validate(object value, Field field, ValidationRule rule)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return Array.Empty<ValidationFailure>();

        if (!rule.Parameters.TryGetValue("maxBytes", out var maxObj) || !long.TryParse(maxObj?.ToString(), out var maxBytes))
            return Array.Empty<ValidationFailure>();

        if (value is string filePath && System.IO.File.Exists(filePath))
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            if (fileInfo.Length > maxBytes)
            {
                return new[]
                {
                    new ValidationFailure(field.Name, $"{field.Label}: Arquivo deve ter no máximo {maxBytes / 1024 / 1024}MB")
                };
            }
        }

        return Array.Empty<ValidationFailure>();
    }
}
