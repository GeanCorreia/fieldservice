namespace FieldService.TemplateService.Types.FieldTypes;

using FieldService.TemplateService.Enums;

public sealed record EmailFieldType : FieldTypeBase
{
    public static readonly EmailFieldType Instance = new();

    private EmailFieldType() { }

    public override string Name => "Email";

    public override IReadOnlySet<ValidatorType> AllowedRules =>
        WithCommonRules(
            ValidatorType.Email,
            ValidatorType.Regex);
}
