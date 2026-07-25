namespace FieldService.Domain.Enums;

public enum ValidatorType
{
    Required,
    MinLength,
    MaxLength,
    Min,
    Max,
    Regex,
    Email,
    Phone,
    DateRange,
    FileSize,
    FileExtension,
    Decimal
}
