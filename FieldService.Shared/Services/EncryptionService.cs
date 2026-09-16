using System.Security.Cryptography;
using System.Text;
using FieldService.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace FieldService.Shared.Services;

public sealed class EncryptionService 
{
    private readonly byte[] _key;

    public EncryptionService(IOptions<EncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Value.Key, nameof(options.Value.Key));
        
        _key = TryParseBase64(options.Value.Key, out var base64Bytes) 
            ? base64Bytes 
            : Encoding.UTF8.GetBytes(options.Value.Key.PadRight(32).Substring(0, 32));

        if (_key.Length != 32)
            throw new ArgumentException("Encryption Key must be exactly 256 bits (32 bytes).");
    }

    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        var cipherTextBytes = new byte[plainBytes.Length];
        var tag = new byte[16]; 

        using var aesGcm = new AesGcm(_key, tag.Length);
        aesGcm.Encrypt(nonce, plainBytes, cipherTextBytes, tag);
        
        var resultBytes = new byte[nonce.Length + tag.Length + cipherTextBytes.Length];
        Buffer.BlockCopy(nonce, 0, resultBytes, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, resultBytes, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherTextBytes, 0, resultBytes, nonce.Length + tag.Length, cipherTextBytes.Length);

        return Convert.ToBase64String(resultBytes);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);

        var encryptedData = Convert.FromBase64String(cipherText);

        if (encryptedData.Length < 28) 
            throw new ArgumentException("Invalid cipherText payload.", nameof(cipherText));

        var nonce = new byte[12];
        var tag = new byte[16];
        var cipherTextBytes = new byte[encryptedData.Length - 28];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, 12);
        Buffer.BlockCopy(encryptedData, 12, tag, 0, 16);
        Buffer.BlockCopy(encryptedData, 28, cipherTextBytes, 0, cipherTextBytes.Length);

        var plainBytes = new byte[cipherTextBytes.Length];

        using var aesGcm = new AesGcm(_key, tag.Length);
        aesGcm.Decrypt(nonce, cipherTextBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static bool TryParseBase64(string input, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(input);
            return true;
        }
        catch
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }
}