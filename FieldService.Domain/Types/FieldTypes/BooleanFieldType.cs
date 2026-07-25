namespace FieldService.Domain.Types.FieldTypes;

using FieldService.Domain.Enums;

public sealed record BooleanFieldType : FieldTypeBase
{
    public static readonly BooleanFieldType Instance = new();

    private BooleanFieldType() { }

    public override string Name => "Boolean";

    public override IReadOnlySet<ValidatorType> AllowedRules => WithCommonRules();
}
