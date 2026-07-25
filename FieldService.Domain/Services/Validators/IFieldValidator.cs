namespace FieldService.Domain.Services.Validators;

using FluentValidation.Results;
using FieldService.Domain.Entities;
using FieldService.Domain.ValueObjects;

public interface IFieldValidator
{
    bool CanValidate(ValidationRule rule);
    ValidationFailure[] Validate(object value, Field field, ValidationRule rule);
}
