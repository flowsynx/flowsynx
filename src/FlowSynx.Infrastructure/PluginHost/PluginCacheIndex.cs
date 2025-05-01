using System.Security.Cryptography;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginCacheIndex
{
    public string UserId { get; }
    public string PluginType { get; }
    public string PluginVersion { get; }

    public PluginCacheIndex(string userId, string pluginType, string pluginVersion)
    {
        UserId = userId;
        PluginType = pluginType;
        PluginVersion = pluginVersion;
    }

    public override string ToString()
    {
        var key = $"{UserId}:{PluginType}:{PluginVersion}";
        return HashKey(key);
    }

    private string HashKey(string? key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            using var hasher = MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(key);
            var hashBytes = hasher.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}