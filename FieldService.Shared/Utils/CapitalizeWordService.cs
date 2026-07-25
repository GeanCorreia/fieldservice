namespace FieldService.Shared.Utils;

public static class CapitalizeWordService
{
    public static string Execute(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            throw new ArgumentException("Word cannot be null or whitespace.", nameof(word));

        var trimmedWord = word.Trim();
        if (trimmedWord.Length == 1)
            return char.ToUpperInvariant(trimmedWord[0]).ToString();

        return char.ToUpperInvariant(trimmedWord[0]) + trimmedWord[1..].ToLowerInvariant();
    }
}
