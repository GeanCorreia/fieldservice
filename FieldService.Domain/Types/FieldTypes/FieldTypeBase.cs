namespace FieldService.Domain.Types.FieldTypes;

using FieldService.Domain.Enums;
using FieldService.Domain.ValueObjects;

public abstract record FieldTypeBase
{
    public abstract string Name { get; }
    public abstract IReadOnlySet<ValidatorType> AllowedRules { get; }
    
    protected static IReadOnlySet<ValidatorType> WithCommonRules(params ValidatorType[] rules)
        => rules.Append(ValidatorType.Required).ToHashSet();

    protected static readonly IReadOnlySet<ValidatorType> UniqueRules = new HashSet<ValidatorType>
    {
        ValidatorType.Required,
        ValidatorType.MinLength,
        ValidatorType.MaxLength,
        ValidatorType.Min,
        ValidatorType.Max,
        ValidatorType.Decimal,
        ValidatorType.DateRange,
        ValidatorType.FileSize,
        ValidatorType.FileExtension
    }.ToHashSet();

    public void ValidateRules(IReadOnlyCollection<ValidationRule> rules)
    {
        ValidateCompatibility(rules);
        ValidateDuplicates(rules);
        ValidateContradictions(rules);
    }

    private void ValidateCompatibility(IReadOnlyCollection<ValidationRule> rules)
    {
        foreach (var rule in rules)
        {
            if (!AllowedRules.Contains(rule.Type))
                throw new InvalidOperationException(
                    $"Rule '{rule.Type}' is not compatible with field type '{Name}'.");
        }
    }

    private static void ValidateDuplicates(IReadOnlyCollection<ValidationRule> rules)
    {
        var seen = new HashSet<ValidatorType>();

        foreach (var rule in rules.Where(r => UniqueRules.Contains(r.Type)))
        {
            if (!seen.Add(rule.Type))
                throw new ArgumentException(
                    $"Duplicate rule '{rule.Type}': this rule can only be defined once.", nameof(rules));
        }
    }

    private static void ValidateContradictions(IReadOnlyCollection<ValidationRule> rules)
    {
        int? minLength = null;
        int? maxLength = null;
        decimal? minValue = null;
        decimal? maxValue = null;

        foreach (var rule in rules)
        {
            switch (rule.Type)
            {
                case ValidatorType.MinLength:
                    if (TryGetIntParameter(rule, "value", out var parsedMin) && parsedMin < 0)
                        throw new ArgumentException("MinLength cannot be negative.", nameof(rules));
                    minLength = parsedMin;
                    break;

                case ValidatorType.MaxLength:
                    if (TryGetIntParameter(rule, "value", out var parsedMax) && parsedMax <= 0)
                        throw new ArgumentException("MaxLength must be greater than zero.", nameof(rules));
                    maxLength = parsedMax;
                    break;

                case ValidatorType.Min:
                    if (TryGetDecimalParameter(rule, "value", out var parsedMinVal))
                        minValue = parsedMinVal;
                    break;

                case ValidatorType.Max:
                    if (TryGetDecimalParameter(rule, "value", out var parsedMaxVal))
                        maxValue = parsedMaxVal;
                    break;

                case ValidatorType.DateRange:
                    if (!TryGetDateTimeParameter(rule, "from", out var from) ||
                        !TryGetDateTimeParameter(rule, "to", out var to))
                        break;

                    if (from >= to)
                        throw new ArgumentException(
                            "DateRange is contradictory: 'from' must be earlier than 'to'.", nameof(rules));
                    break;

                case ValidatorType.Decimal:
                    if (TryGetIntParameter(rule, "places", out var places) && places < 0)
                        throw new ArgumentException("Decimal places cannot be negative.", nameof(rules));
                    break;
            }
        }

        if (minLength.HasValue && maxLength.HasValue && minLength.Value > maxLength.Value)
            throw new ArgumentException(
                $"Contradictory rules: MinLength ({minLength}) cannot be greater than MaxLength ({maxLength}).", nameof(rules));

        if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
            throw new ArgumentException(
                $"Contradictory rules: Min ({minValue}) cannot be greater than Max ({maxValue}).", nameof(rules));
    }

    private static bool TryGetIntParameter(ValidationRule rule, string parameterName, out int value)
    {
        value = default;
        return rule.Parameters.TryGetValue(parameterName, out var rawValue) &&
               int.TryParse(rawValue?.ToString(), out value);
    }

    private static bool TryGetDecimalParameter(ValidationRule rule, string parameterName, out decimal value)
    {
        value = default;
        return rule.Parameters.TryGetValue(parameterName, out var rawValue) &&
               decimal.TryParse(rawValue?.ToString(), out value);
    }

    private static bool TryGetDateTimeParameter(ValidationRule rule, string parameterName, out DateTime value)
    {
        value = default;
        return rule.Parameters.TryGetValue(parameterName, out var rawValue) &&
               DateTime.TryParse(rawValue?.ToString(), out value);
    }
}
