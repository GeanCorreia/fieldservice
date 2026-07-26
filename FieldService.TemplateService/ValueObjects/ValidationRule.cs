namespace FieldService.Shared.Types;

using FieldService.TemplateService.Enums;

public class ValidationRule
{
    public ValidatorType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();

    public static ValidationRule Required() 
        => new() { Type = ValidatorType.Required };

    public static ValidationRule MinLength(int length) 
        => new() { Type = ValidatorType.MinLength, Parameters = new() { { "value", length } } };

    public static ValidationRule MaxLength(int length) 
        => new() { Type = ValidatorType.MaxLength, Parameters = new() { { "value", length } } };

    public static ValidationRule Min(decimal value) 
        => new() { Type = ValidatorType.Min, Parameters = new() { { "value", value } } };

    public static ValidationRule Max(decimal value) 
        => new() { Type = ValidatorType.Max, Parameters = new() { { "value", value } } };

    public static ValidationRule Regex(string pattern) 
        => new() { Type = ValidatorType.Regex, Parameters = new() { { "pattern", pattern } } };

    public static ValidationRule Email() 
        => new() { Type = ValidatorType.Email };

    public static ValidationRule Phone() 
        => new() { Type = ValidatorType.Phone };

    public static ValidationRule FileSize(long maxBytes) 
        => new() { Type = ValidatorType.FileSize, Parameters = new() { { "maxBytes", maxBytes } } };

    public static ValidationRule FileExtension(params string[] extensions) 
        => new() { Type = ValidatorType.FileExtension, Parameters = new() { { "extensions", extensions } } };

    public static ValidationRule DateRange(DateTime from, DateTime to)
    {
        return new() 
        { 
            Type = ValidatorType.DateRange, 
            Parameters = new() { { "from", from }, { "to", to } } 
        };
    }

    public static ValidationRule Decimal(int places)
    {
        return new()
        {
            Type = ValidatorType.Decimal,
            Parameters = new() { { "places", places } }
        };
    }
}
