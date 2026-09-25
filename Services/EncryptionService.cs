using System.Security.Cryptography;
using System.Text;

namespace SecurityProject.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IConfiguration config)
    {
        this._key = Convert.FromBase64String(config["EncryptionKey"]!);
        this._iv = Convert.FromBase64String(config["EncryptionIV"]!);
    }
    public string Decrypt(string ciphertext)
    {
        var aes = Aes.Create();
        aes.Key = this._key;
        aes.IV = this._iv;

        var ciphertextBytes = Convert.FromBase64String(ciphertext);
        var decryptor = aes.CreateDecryptor();

        var result = decryptor.TransformFinalBlock(ciphertextBytes, 0, ciphertextBytes.Length);
        return Encoding.UTF8.GetString(result);
    }

    public  string Encrypt(string plaintext)
    {
        var aes = Aes.Create();
        aes.Key = this._key;
        aes.IV = this._iv;

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var encryptor = aes.CreateEncryptor();
        var result = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        return Convert.ToBase64String(result);
    }
}