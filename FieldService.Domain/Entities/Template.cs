namespace FieldService.Domain.Entities;

using FieldService.Domain.Enums;
using FieldService.Domain.ValueObjects;
using FieldService.Shared.Utils;
public class Template
{
    public Guid Id { get; } 
    public Guid TenantId { get; }
    public string Name { get; private set; }
    public ICollection<TemplateVersion> Versions { get; }
    
    protected Template( ){}
    
    public Template(
        Guid id, 
        Guid tenantId, 
        string name, 
        ICollection<TemplateVersion> versions)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        Versions = versions ?? throw new ArgumentNullException(nameof(versions));
    }

    public void SetName(string name)
    {
        Name = NormalizeTextService.Execute(name, nameof(name), "Name cannot be null or whitespace.");
    }
    public TemplateVersion? GetActiveVersion()
    {
        return Versions.FirstOrDefault(v => v.IsActive);
    }

    public TemplateVersion? GetVersion(string version)
    {
        return Versions.FirstOrDefault(v => v.Version.Equals(version));
    }

    public TemplateVersion? GetVersion(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return Versions.FirstOrDefault(v => v.Version == version);
    }

    
    public TemplateUpdateResult UpdateFields(
        Guid expectedTemplateVersionId,
        IEnumerable<Field> proposedFields,
        DateTime changedAt,
        string? description = null,
        TemplateUI? templateUi = null)
    {
        ArgumentNullException.ThrowIfNull(proposedFields);

        var currentVersion = GetActiveVersion()
            ?? throw new InvalidOperationException("Template has no active version.");

        if (currentVersion.Id != expectedTemplateVersionId)
            throw new InvalidOperationException(
                $"The expected version '{expectedTemplateVersionId}' is not the current active version.");

        var proposedFieldList = proposedFields as List<Field> ?? proposedFields.ToList();
        if (proposedFieldList.Count == 0)
            throw new ArgumentException("Fields cannot be empty.", nameof(proposedFields));
        ValidateFieldNames(proposedFieldList);

        var changeType = ClassifyChange(currentVersion.Fields, proposedFieldList);
        if (changeType == VersionChangeType.None)
            return new TemplateUpdateResult(VersionChangeType.None, currentVersion.Version, currentVersion);

        var nextVersion = changeType switch
        {
            VersionChangeType.Patch => Version.PlusPatch(currentVersion.Version),
            VersionChangeType.Minor => Version.PlusMinor(currentVersion.Version),
            VersionChangeType.Major => Version.PlusMajor(currentVersion.Version),
            _ => throw new InvalidOperationException($"Unsupported change type '{changeType}'.")
        };

        currentVersion.Deactivate(changedAt);
        var nextTemplateVersion = TemplateVersion.Create(
            changedAt, 
            proposedFieldList, 
            nextVersion, 
            description ?? currentVersion.Description,
            templateUi?.Clone() ?? currentVersion.Ui.Clone());
        
        Versions.Add(nextTemplateVersion);

        return new TemplateUpdateResult(changeType, nextVersion, nextTemplateVersion);
    }

    private static VersionChangeType ClassifyChange(
        IReadOnlyCollection<Field> currentFields, 
        IReadOnlyCollection<Field> proposedFields)
    {
        var currentById = currentFields.ToDictionary(field => field.Id);
        var proposedById = proposedFields.ToDictionary(field => field.Id);
        var changeType = VersionChangeType.None;

        foreach (var currentField in currentFields)
        {
            if (!proposedById.ContainsKey(currentField.Id))
                return VersionChangeType.Major;
        }

        foreach (var proposedField in proposedFields)
        {
            if (!currentById.TryGetValue(proposedField.Id, out var currentField))
            {
                changeType = Max(changeType, HasRequiredRule(proposedField) ? 
                    VersionChangeType.Major : VersionChangeType.Minor);
                continue;
            }

            changeType = Max(changeType, ClassifyFieldChange(currentField, proposedField));
            if (changeType == VersionChangeType.Major)
                return VersionChangeType.Major;
        }

        return changeType;
    }

    private static VersionChangeType ClassifyFieldChange(Field currentField, Field proposedField)
    {
        if (!string.Equals(currentField.Name, proposedField.Name, StringComparison.Ordinal))
            return VersionChangeType.Major;

        if (currentField.Type.GetType() != proposedField.Type.GetType())
            return VersionChangeType.Major;

        var ruleChange = ClassifyRuleChanges(currentField.Rules, proposedField.Rules);
        if (ruleChange == VersionChangeType.Major)
            return VersionChangeType.Major;

        var fieldChange = VersionChangeType.None;

        if (!Equals(currentField.DefaultValue, proposedField.DefaultValue))
            fieldChange = Max(fieldChange, VersionChangeType.Minor);

        if (currentField.Order != proposedField.Order ||
            currentField.Visible != proposedField.Visible ||
            !string.Equals(currentField.Label, proposedField.Label, StringComparison.Ordinal) ||
            !string.Equals(currentField.Placeholder, proposedField.Placeholder, StringComparison.Ordinal) ||
            !string.Equals(currentField.Description, proposedField.Description, StringComparison.Ordinal))
        {
            fieldChange = Max(fieldChange, VersionChangeType.Patch);
        }

        return Max(fieldChange, ruleChange);
    }

    private static VersionChangeType ClassifyRuleChanges(
        IReadOnlyCollection<ValidationRule> currentRules,
        IReadOnlyCollection<ValidationRule> proposedRules)
    {
        var currentRequired = currentRules.Any(rule => rule.Type == ValidatorType.Required);
        var proposedRequired = proposedRules.Any(rule => rule.Type == ValidatorType.Required);

        var changeType = VersionChangeType.None;
        if (!currentRequired && proposedRequired)
            changeType = Max(changeType, VersionChangeType.Major);
        if (currentRequired && !proposedRequired)
            changeType = Max(changeType, VersionChangeType.Minor);

        var currentNonRequired = currentRules.Where(rule => rule.Type != ValidatorType.Required).ToList();
        var proposedNonRequired = proposedRules.Where(rule => rule.Type != ValidatorType.Required).ToList();

        if (AreRuleSetsEquivalent(currentNonRequired, proposedNonRequired))
            return changeType;

        var currentMap = currentNonRequired.ToDictionary(rule => rule.Type);
        var proposedMap = proposedNonRequired.ToDictionary(rule => rule.Type);

        foreach (var type in currentMap.Keys)
        {
            if (!proposedMap.ContainsKey(type))
                changeType = Max(changeType, VersionChangeType.Minor);
        }

        foreach (var type in proposedMap.Keys)
        {
            if (!currentMap.ContainsKey(type))
                changeType = Max(changeType, VersionChangeType.Major);
        }

        foreach (var type in currentMap.Keys.Intersect(proposedMap.Keys))
        {
            var currentRule = currentMap[type];
            var proposedRule = proposedMap[type];
            if (AreRulesEquivalent(currentRule, proposedRule))
                continue;

            changeType = Max(changeType, ClassifyRuleParameterChange(type, currentRule, proposedRule));
            if (changeType == VersionChangeType.Major)
                return VersionChangeType.Major;
        }

        return changeType;
    }

    private static VersionChangeType ClassifyRuleParameterChange(
        ValidatorType ruleType,
        ValidationRule currentRule,
        ValidationRule proposedRule)
    {
        switch (ruleType)
        {
            case ValidatorType.MinLength:
                return CompareNumericThreshold(currentRule, proposedRule, "value", strictWhenIncreases: true);
            case ValidatorType.MaxLength:
                return CompareNumericThreshold(currentRule, proposedRule, "value", strictWhenIncreases: false);
            case ValidatorType.Min:
                return CompareNumericThreshold(currentRule, proposedRule, "value", strictWhenIncreases: true);
            case ValidatorType.Max:
                return CompareNumericThreshold(currentRule, proposedRule, "value", strictWhenIncreases: false);
            case ValidatorType.Decimal:
                return CompareNumericThreshold(currentRule, proposedRule, "places", strictWhenIncreases: false);
            case ValidatorType.FileSize:
                return CompareNumericThreshold(currentRule, proposedRule, "maxBytes", strictWhenIncreases: false);
            case ValidatorType.FileExtension:
                return CompareExtensions(currentRule, proposedRule);
            default:
                return VersionChangeType.Major;
        }
    }

    private static VersionChangeType CompareExtensions(ValidationRule currentRule, ValidationRule proposedRule)
    {
        var currentSet = GetArrayParameter(currentRule, "extensions");
        var proposedSet = GetArrayParameter(proposedRule, "extensions");

        if (currentSet.SetEquals(proposedSet))
            return VersionChangeType.None;

        if (currentSet.IsSupersetOf(proposedSet))
            return VersionChangeType.Major;

        if (proposedSet.IsSupersetOf(currentSet))
            return VersionChangeType.Minor;

        return VersionChangeType.Major;
    }

    private static VersionChangeType CompareNumericThreshold(
        ValidationRule currentRule,
        ValidationRule proposedRule,
        string parameterName,
        bool strictWhenIncreases)
    {
        if (!TryGetDecimalParameter(currentRule, parameterName, out var currentValue) ||
            !TryGetDecimalParameter(proposedRule, parameterName, out var proposedValue))
        {
            return VersionChangeType.Major;
        }

        if (currentValue == proposedValue)
            return VersionChangeType.None;

        var increased = proposedValue > currentValue;
        var isStricter = strictWhenIncreases ? increased : !increased;
        return isStricter ? VersionChangeType.Major : VersionChangeType.Minor;
    }

    private static bool TryGetDecimalParameter(ValidationRule rule, string parameterName, out decimal value)
    {
        value = default;
        return rule.Parameters.TryGetValue(parameterName, out var rawValue) &&
               decimal.TryParse(rawValue?.ToString(), out value);
    }

    private static HashSet<string> GetArrayParameter(ValidationRule rule, string parameterName)
    {
        if (!rule.Parameters.TryGetValue(parameterName, out var rawValue) || rawValue is not IEnumerable<string> values)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool AreRuleSetsEquivalent(
        IReadOnlyCollection<ValidationRule> leftRules,
        IReadOnlyCollection<ValidationRule> rightRules)
    {
        if (leftRules.Count != rightRules.Count)
            return false;

        var rightByType = rightRules.ToDictionary(rule => rule.Type);
        foreach (var leftRule in leftRules)
        {
            if (!rightByType.TryGetValue(leftRule.Type, out var rightRule))
                return false;

            if (!AreRulesEquivalent(leftRule, rightRule))
                return false;
        }

        return true;
    }

    private static bool AreRulesEquivalent(ValidationRule leftRule, ValidationRule rightRule)
    {
        if (leftRule.Type != rightRule.Type || leftRule.Parameters.Count != rightRule.Parameters.Count)
            return false;

        foreach (var (key, leftValue) in leftRule.Parameters)
        {
            if (!rightRule.Parameters.TryGetValue(key, out var rightValue))
                return false;

            if (!AreParameterValuesEqual(leftValue, rightValue))
                return false;
        }

        return true;
    }

    private static bool AreParameterValuesEqual(object? left, object? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        if (left is IEnumerable<string> leftValues && right is IEnumerable<string> rightValues)
            return leftValues.SequenceEqual(rightValues, StringComparer.OrdinalIgnoreCase);

        return string.Equals(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static VersionChangeType Max(VersionChangeType left, VersionChangeType right)
        => (VersionChangeType)Math.Max((int)left, (int)right);

    private static bool HasRequiredRule(Field field)
        => field.Rules.Any(rule => rule.Type == ValidatorType.Required);

    private static void ValidateFieldNames(IReadOnlyCollection<Field> fields)
    {
        var reservedKeys = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            var aliases = new[] { field.Name, field.Label }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var alias in aliases)
            {
                if (reservedKeys.TryGetValue(alias, out var ownerFieldId) && ownerFieldId != field.Id)
                {
                    throw new InvalidOperationException(
                        $"Field name/label collision detected: '{alias}' is already used by field '{ownerFieldId}'.");
                }

                reservedKeys[alias] = field.Id;
            }
        }
    }
}
