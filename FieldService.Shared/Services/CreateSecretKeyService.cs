using System.Security.Cryptography;

namespace FieldService.Shared.Services;

public static class CreateSecretKeyService
{

    public static string CreateSecretKey(int byteLength = 32)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(byteLength);
        
        return Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
    
    public static string CreateHexString(int byteLength = 32)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToHexStringLower(randomBytes);
    }
}