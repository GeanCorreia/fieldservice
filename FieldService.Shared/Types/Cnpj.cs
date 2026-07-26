namespace FieldService.Shared.Types;

using System.Text.RegularExpressions;

public record Cnpj : IDocument
{
    private readonly string _value;

    public string Value => _value;
    public string FormattedValue => Format(_value);
    public bool IsValid => ValidateCnpj(_value);

    private Cnpj(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CNPJ cannot be empty.", nameof(value));

        var normalized = NormalizeInput(value);
        if (normalized.Length != 14)
            throw new ArgumentException("CNPJ must contain exactly 14 digits.", nameof(value));

        _value = normalized;
    }

    public static Cnpj FromString(string value)
    {
        return new Cnpj(value);
    }

    public static bool TryParse(string value, out Cnpj? cnpj)
    {
        cnpj = null;
        try
        {
            cnpj = new Cnpj(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeInput(string input)
    {
        return Regex.Replace(input.Trim(), @"[^\d]", "");
    }

    private static bool ValidateCnpj(string cnpj)
    {
        if (cnpj.Length != 14 || !cnpj.All(char.IsDigit))
            return false;

        if (cnpj.All(c => c == cnpj[0]))
            return false;

        int[] multiplier1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplier2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        string tempCnpj = cnpj.Substring(0, 12);
        int sum = 0;

        for (int i = 0; i < 12; i++)
            sum += int.Parse(tempCnpj[i].ToString()) * multiplier1[i];

        int remainder = sum % 11;
        remainder = remainder < 2 ? 0 : 11 - remainder;

        if (remainder != int.Parse(cnpj[12].ToString()))
            return false;

        tempCnpj = cnpj.Substring(0, 13);
        sum = 0;

        for (int i = 0; i < 13; i++)
            sum += int.Parse(tempCnpj[i].ToString()) * multiplier2[i];

        remainder = sum % 11;
        remainder = remainder < 2 ? 0 : 11 - remainder;

        return remainder == int.Parse(cnpj[13].ToString());
    }

    private static string Format(string cnpj)
    {
        if (cnpj.Length != 14)
            return cnpj;

        return $"{cnpj.Substring(0, 2)}.{cnpj.Substring(2, 3)}.{cnpj.Substring(5, 3)}/{cnpj.Substring(8, 4)}-{cnpj.Substring(12, 2)}";
    }

    public override string ToString() => FormattedValue;
}
