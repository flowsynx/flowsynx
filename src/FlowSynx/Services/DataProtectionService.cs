using FlowSynx.Infrastructure.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace FlowSynx.Services;

public sealed class DataProtectionService : IDataProtectionService
{
    private readonly IDataProtector _protector;

    public DataProtectionService(IDataProtector protector)
    {
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
    }

    public string Protect(string plaintext)
        => string.IsNullOrEmpty(plaintext) ? plaintext : _protector.Protect(plaintext);

    public string Unprotect(string protectedData)
        => string.IsNullOrEmpty(protectedData) ? protectedData : _protector.Unprotect(protectedData);
}