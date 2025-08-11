namespace application.Contracts.Services
{
    public interface IEncryptionService
    {
        string Decrypt(string cipherText);

        string Encrypt(string plainText);

        string HashPassword(string password);

        string HashPIN(string pin);

        bool VerifyPassword(string password, string hash);

        bool VerifyPIN(string pin, string hash);
    }
}