namespace FlowSynx.Infrastructure.Security.Cryptography;

public interface IDataProtectionService
{
    string Protect(string plaintext);
    string Unprotect(string protectedData);
}