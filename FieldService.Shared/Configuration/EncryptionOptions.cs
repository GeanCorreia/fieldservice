namespace FieldService.Shared.Configuration;

public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";
    public string Key { get; init; } = string.Empty;
}