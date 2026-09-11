using System.Security.Cryptography;
using System.Text;
using FieldService.Shared.Interfaces;

namespace FieldService.Shared.Services;

public sealed class HashService : IHashService
{
    public string Generate(string value) => GenerateHash(value);

    public static string CreateHashMd5(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = MD5.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string CreateHashMd5(Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var originalPosition = content.CanSeek ? content.Position : 0;

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(content);

        if (content.CanSeek)
        {
            content.Position = originalPosition;
        }

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string GenerateHash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
