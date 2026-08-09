using System.Text.RegularExpressions;

namespace FieldService.Shared.Types;

public sealed record Phone
{
    private static readonly Regex DigitsOnlyRegex = new(@"\D", RegexOptions.Compiled);

    // DDDs válidos do Brasil (atualizado 2024)
    private static readonly HashSet<string> ValidDDDs = new()
    {
        // Região Sul
        "41", "42", "43", "44", "45", "46", // Paraná
        "47", "48", "49", // Santa Catarina
        "51", "53", "54", "55", // Rio Grande do Sul
        
        // Região Sudeste
        "11", "12", "13", "14", "15", "16", "17", "18", "19", // São Paulo
        "21", "22", "24", // Rio de Janeiro
        "27", "28", // Espírito Santo
        "31", "32", "33", "34", "35", "37", "38", // Minas Gerais
        
        // Região Centro-Oeste
        "61", // Distrito Federal
        "62", "64", // Goiás
        "65", "66", // Mato Grosso
        "67", // Mato Grosso do Sul
        
        // Região Nordeste
        "71", "73", "74", "75", "77", // Bahia
        "79", // Sergipe
        "81", "87", // Pernambuco
        "82", // Alagoas
        "83", // Paraíba
        "84", // Rio Grande do Norte
        "85", "88", // Ceará
        "86", "89", // Piauí
        "98", "99", // Maranhão
        
        // Região Norte
        "91", "93", "94", // Pará
        "92", "97", // Amazonas
        "95", // Roraima
        "96", // Amapá
        "63", // Tocantins
        "68", // Acre
        "69"  // Rondônia
    };

    public string DDD { get; }
    public string Number { get; }
    public bool IsMobile { get; }

    public Phone(string ddd, string number)
    {
        if (string.IsNullOrWhiteSpace(ddd))
            throw new ArgumentException("DDD cannot be empty", nameof(ddd));
        
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Number cannot be empty", nameof(number));

        var cleanDDD = DigitsOnlyRegex.Replace(ddd.Trim(), "");
        var cleanNumber = DigitsOnlyRegex.Replace(number.Trim(), "");

        if (cleanDDD.Length != 2)
            throw new ArgumentException("DDD must have exactly 2 digits", nameof(ddd));

        if (!ValidDDDs.Contains(cleanDDD))
            throw new ArgumentException($"Invalid DDD: {cleanDDD}", nameof(ddd));

        if (cleanNumber.Length < 8 || cleanNumber.Length > 9)
            throw new ArgumentException(
                "Number must have 8 digits (landline) or 9 digits (mobile)", 
                nameof(number)
            );

        // Celular começa com 9 e tem 9 dígitos
        var isMobile = cleanNumber.Length == 9 && cleanNumber[0] == '9';

        // Fixo tem 8 dígitos e não começa com 9
        if (cleanNumber.Length == 8 && cleanNumber[0] == '9')
            throw new ArgumentException(
                "Landline numbers cannot start with 9", 
                nameof(number)
            );

        DDD = cleanDDD;
        Number = cleanNumber;
        IsMobile = isMobile;
    }

    // Construtor que aceita tudo junto: "11987654321" ou "(11) 98765-4321"
    public Phone(string fullPhone) : this(
        ExtractDDD(fullPhone),
        ExtractNumber(fullPhone)
    )
    { }

    private static string ExtractDDD(string fullPhone)
    {
        if (string.IsNullOrWhiteSpace(fullPhone))
            throw new ArgumentException("Phone cannot be empty", nameof(fullPhone));

        var digitsOnly = DigitsOnlyRegex.Replace(fullPhone.Trim(), "");

        if (digitsOnly.Length < 10 || digitsOnly.Length > 11)
            throw new ArgumentException(
                "Phone must have 10 digits (DDD + landline) or 11 digits (DDD + mobile)", 
                nameof(fullPhone)
            );

        return digitsOnly.Substring(0, 2);
    }

    private static string ExtractNumber(string fullPhone)
    {
        var digitsOnly = DigitsOnlyRegex.Replace(fullPhone.Trim(), "");
        return digitsOnly.Substring(2);
    }

    public string ToDigitsOnly() => $"{DDD}{Number}";

    // Formata como (11) 98765-4321 ou (11) 3456-7890
    public string ToFormatted()
    {
        if (IsMobile)
            return $"({DDD}) {Number.Substring(0, 5)}-{Number.Substring(5)}";
        
        return $"({DDD}) {Number.Substring(0, 4)}-{Number.Substring(4)}";
    }

    public static implicit operator string(Phone phone) => phone.ToDigitsOnly();
    
    public static explicit operator Phone(string value) => new(value);

    public override string ToString() => ToFormatted();
}
