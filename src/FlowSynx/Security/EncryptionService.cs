using FlowSynx.Application.Configuration.Core.Security;
using System.Security.Cryptography;

namespace FlowSynx.Security;

public class EncryptionService : IEncryptionService
{
    private readonly byte[]? _key;

    public EncryptionService(string? encryptionKey)
    {
        if (!string.IsNullOrEmpty(encryptionKey))
        {
            _key = Convert.FromBase64String(encryptionKey);
            if (_key.Length != 32)
                throw new InvalidOperationException("Encryption Key must be 256 bits (32 bytes).");
        }
        else
        {
            _key = null; // no encryption
        }
    }

    public string Encrypt(string plainText)
    {
        // If no key, return plain text directly
        if (_key == null)
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        var iv = aes.IV;
        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var ms = new MemoryStream();
        ms.Write(iv, 0, iv.Length); // prepend IV
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        // If no key, return cipherText directly
        if (_key == null)
            return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[aes.BlockSize / 8];
        Array.Copy(fullCipher, iv, iv.Length);

        var cipher = new byte[fullCipher.Length - iv.Length];
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}