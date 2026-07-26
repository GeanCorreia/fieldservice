namespace FieldService.TemplateService.Services.Validators;

using FluentValidation.Results;
using FieldService.TemplateService.Entities;
using FieldService.Shared.Types;

public interface IFieldValidator
{
    bool CanValidate(ValidationRule rule);
    ValidationFailure[] Validate(object value, Field field, ValidationRule rule);
}
