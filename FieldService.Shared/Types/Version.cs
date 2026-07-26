namespace FieldService.Shared.Types;

public record Version(
    int Major,
    int Minor,
    int Patch)
{
    public static Version FromString(string version)
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

        return new Version(major, minor, patch);
    }
    
    public static Version PlusMajor(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return new Version(version.Major + 1, 0, 0);
    }

    public static Version PlusMinor(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return new Version(version.Major, version.Minor + 1, 0);
    }

    public static Version PlusPatch(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return new Version(version.Major, version.Minor, version.Patch + 1);
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