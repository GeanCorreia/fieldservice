namespace FieldService.TemplateService.Services.Validators;

using FieldService.TemplateService.Entities;
using FieldService.TemplateService.Enums;

public interface IFieldValidationService
{
    ValidationResult Validate(object value, Field field);
    ValidationResult ValidateTemplateData(Dictionary<string, object> data, TemplateVersion templateVersion);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class FieldValidationService : IFieldValidationService
{
    private readonly IValidatorFactory _validatorFactory;

    public FieldValidationService(IValidatorFactory validatorFactory)
    {
        _validatorFactory = validatorFactory ?? throw new ArgumentNullException(nameof(validatorFactory));
    }

    public ValidationResult Validate(object value, Field field)
    {
        var result = new ValidationResult { IsValid = true };

        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            if (field.Rules.Any(r => r.Type == ValidatorType.Required))
            {
                result.IsValid = false;
                result.Errors.Add($"{field.Label} é obrigatório");
                return result;
            }
            return result;
        }

        foreach (var rule in field.Rules)
        {
            var validator = _validatorFactory.GetValidator(rule);
            if (validator != null)
            {
                var failures = validator.Validate(value, field, rule);
                if (failures.Any())
                {
                    result.IsValid = false;
                    result.Errors.AddRange(failures.Select(f => f.ErrorMessage));
                }
            }
        }

        return result;
    }

    public ValidationResult ValidateTemplateData(Dictionary<string, object> data, TemplateVersion templateVersion)
    {
        var result = new ValidationResult { IsValid = true };

        foreach (var field in templateVersion.Fields)
        {
            if (data.TryGetValue(field.Name, out var value))
            {
                var fieldResult = Validate(value, field);
                if (!fieldResult.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(fieldResult.Errors);
                }
            }
            else if (field.Rules.Any(r => r.Type == ValidatorType.Required))
            {
                result.IsValid = false;
                result.Errors.Add($"{field.Label} é obrigatório");
            }
        }

        return result;
    }
}
