using application.Contracts.Services;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace infrastructure.Services;

internal class EncryptionService : IEncryptionService
{
    private readonly string _encryptionKey;

    public EncryptionService(IConfiguration configuration)
    {
        _encryptionKey = configuration["Security:EncryptionKey"] ?? throw new ArgumentNullException(nameof(_encryptionKey));

        if (_encryptionKey.Length < 32)
            throw new ArgumentException("Encryption key must be at least 32 characters long", nameof(_encryptionKey));
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentException("Cipher text cannot be null or empty", nameof(cipherText));

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey[..32]);

        var iv = new byte[aes.IV.Length];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Array.Copy(fullCipher, iv, iv.Length);
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        var decryptor = aes.CreateDecryptor();

        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey[..32]); // Use first 32 chars as key
        aes.GenerateIV();

        var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        using var msEncrypt = new MemoryStream();
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public string HashPIN(string pin)
    {
        if (string.IsNullOrEmpty(pin))
            throw new ArgumentException("PIN cannot be null or empty", nameof(pin));

        return BCrypt.Net.BCrypt.HashPassword(pin, BCrypt.Net.BCrypt.GenerateSalt(10));
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    public bool VerifyPIN(string pin, string hash)
    {
        if (string.IsNullOrEmpty(pin) || string.IsNullOrEmpty(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(pin, hash);
        }
        catch
        {
            return false;
        }
    }
}