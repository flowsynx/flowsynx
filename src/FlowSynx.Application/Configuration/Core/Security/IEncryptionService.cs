namespace FlowSynx.Application.Configuration.Core.Security;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}