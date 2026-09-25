namespace SecurityProject.Services;

public interface IEncryptionService
{
    public string Encrypt(string plaintext);
    public string Decrypt(string ciphertext);
}