namespace FieldService.TemplateService.Types.FieldTypes;

using FieldService.TemplateService.Enums;

public sealed record FileFieldType : FieldTypeBase
{
    public static readonly FileFieldType Instance = new();

    private FileFieldType() { }

    public override string Name => "File";

    public override IReadOnlySet<ValidatorType> AllowedRules =>
        WithCommonRules(
            ValidatorType.FileSize,
            ValidatorType.FileExtension);
}
