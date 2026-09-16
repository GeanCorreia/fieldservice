using System.Security.Cryptography;
using System.Text;
using FieldService.Shared.Interfaces;

namespace FieldService.Shared.Services;

public sealed class HashService : IHashService
{
    public string Generate(string value) => CreateHashSha256(value);

    public static string CreateHashMd5(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = MD5.HashData(bytes);
        
        return Convert.ToBase64String(hash);
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
        
        return Convert.ToBase64String(hash);
    }

    public static string CreateHashSha256(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}