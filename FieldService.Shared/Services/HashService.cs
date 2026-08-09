using System.Security.Cryptography;
using System.Text;
using FieldService.Shared.Interfaces;

namespace FieldService.Shared.Services;

public sealed class HashService : IHashService
{
    public string Generate(string value) => GenerateHash(value);

    public static string GenerateHash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
