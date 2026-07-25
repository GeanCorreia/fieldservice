namespace FieldService.Shared.Utils;

public static class NormalizeTextService
{
    public static string Execute(string value, string parameterName, string emptyMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(emptyMessage, parameterName);

        var words = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            words[i] = CapitalizeWordService.Execute(words[i]);
        }

        return string.Join(" ", words);
    }
}
