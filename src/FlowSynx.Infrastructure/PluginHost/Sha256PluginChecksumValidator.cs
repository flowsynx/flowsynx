using System.Security.Cryptography;

namespace FlowSynx.Infrastructure.PluginHost;

public class Sha256PluginChecksumValidator : IPluginChecksumValidator
{
    public bool ValidateChecksum(byte[] data, string expectedChecksum)
    {
        string computedChecksum = ComputeChecksum(data);
        return computedChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    private string ComputeChecksum(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }

}