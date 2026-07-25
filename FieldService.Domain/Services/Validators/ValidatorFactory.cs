namespace FieldService.Domain.Services.Validators;

using FieldService.Domain.Entities;
using FieldService.Domain.ValueObjects;

public interface IValidatorFactory
{
    IFieldValidator[] GetValidators();
    IFieldValidator? GetValidator(ValidationRule rule);
}

public class ValidatorFactory : IValidatorFactory
{
    private readonly IFieldValidator[] _validators;

    public ValidatorFactory()
    {
        _validators = new IFieldValidator[]
        {
            new IsRequiredValidator(),
            new MinLengthValidator(),
            new MaxLengthValidator(),
            new MinValueValidator(),
            new MaxValueValidator(),
            new RegexValidator(),
            new EmailValidator(),
            new PhoneValidator(),
            new FileSizeValidator(),
            new FileExtensionValidator(),
            new DateRangeValidator(),
            new DecimalPlacesValidator()
        };
    }

    public IFieldValidator[] GetValidators() => _validators;

    public IFieldValidator? GetValidator(ValidationRule rule)
        => _validators.FirstOrDefault(v => v.CanValidate(rule));
}
