namespace FieldService.Shared.Types;

public record SchemaVersion(
    int Major,
    int Minor,
    int Patch)
{
    public static SchemaVersion FromString(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be null or whitespace.", nameof(version));

        var parts = version.Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
        {
            throw new ArgumentException("Version must follow the format 'major.minor.patch'.", nameof(version));
        }

        return new SchemaVersion(major, minor, patch);
    }
    
    public static SchemaVersion PlusMajor(SchemaVersion schemaVersion)
    {
        ArgumentNullException.ThrowIfNull(schemaVersion);
        return new SchemaVersion(schemaVersion.Major + 1, 0, 0);
    }

    public static SchemaVersion PlusMinor(SchemaVersion schemaVersion)
    {
        ArgumentNullException.ThrowIfNull(schemaVersion);
        return new SchemaVersion(schemaVersion.Major, schemaVersion.Minor + 1, 0);
    }

    public static SchemaVersion PlusPatch(SchemaVersion schemaVersion)
    {
        ArgumentNullException.ThrowIfNull(schemaVersion);
        return new SchemaVersion(schemaVersion.Major, schemaVersion.Minor, schemaVersion.Patch + 1);
    }

    public bool Equals(string version)
    {
        try
        {
            return Equals(FromString(version));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}