namespace FieldService.Shared.Types;

using System.Text.RegularExpressions;

public record Cpf : IDocument
{
    private readonly string _value;

    public string Value => _value;
    public string FormattedValue => Format(_value);
    public bool IsValid => ValidateCpf(_value);

    private Cpf(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CPF cannot be empty.", nameof(value));

        var normalized = NormalizeInput(value);
        if (normalized.Length != 11)
            throw new ArgumentException("CPF must contain exactly 11 digits.", nameof(value));

        _value = normalized;
    }

    public static Cpf FromString(string value)
    {
        return new Cpf(value);
    }

    public static bool TryParse(string value, out Cpf? cpf)
    {
        cpf = null;
        try
        {
            cpf = new Cpf(value);
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

    private static bool ValidateCpf(string cpf)
    {
        if (cpf.Length != 11 || !cpf.All(char.IsDigit))
            return false;

        if (cpf.All(c => c == cpf[0]))
            return false;

        int[] multiplier1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplier2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        string tempCpf = cpf.Substring(0, 9);
        int sum = 0;

        for (int i = 0; i < 9; i++)
            sum += int.Parse(tempCpf[i].ToString()) * multiplier1[i];

        int remainder = sum % 11;
        remainder = remainder < 2 ? 0 : 11 - remainder;

        if (remainder != int.Parse(cpf[9].ToString()))
            return false;

        tempCpf = cpf.Substring(0, 10);
        sum = 0;

        for (int i = 0; i < 10; i++)
            sum += int.Parse(tempCpf[i].ToString()) * multiplier2[i];

        remainder = sum % 11;
        remainder = remainder < 2 ? 0 : 11 - remainder;

        return remainder == int.Parse(cpf[10].ToString());
    }

    private static string Format(string cpf)
    {
        if (cpf.Length != 11)
            return cpf;

        return $"{cpf.Substring(0, 3)}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-{cpf.Substring(9, 2)}";
    }

    public override string ToString() => FormattedValue;
}
