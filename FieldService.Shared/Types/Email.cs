using System.Text.RegularExpressions;

namespace FieldService.Shared.Types;

public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1)
    );

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 254)
            throw new ArgumentException("Email cannot exceed 254 characters", nameof(value));

        if (!EmailRegex.IsMatch(normalized))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));

        Value = normalized;
    }

    public static implicit operator string(Email email) => email.Value;
    
    public static explicit operator Email(string value) => new(value);

    public override string ToString() => Value;
}
