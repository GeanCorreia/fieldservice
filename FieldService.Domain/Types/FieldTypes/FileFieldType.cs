namespace FieldService.Domain.Types.FieldTypes;

using FieldService.Domain.Enums;

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
