using FlowSynx.Application.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginCacheKeyGeneratorService: IPluginCacheKeyGeneratorService
{
    private readonly IJsonSerializer _jsonSerializer;

    public PluginCacheKeyGeneratorService(IJsonSerializer serializer)
    {
        _jsonSerializer = serializer;
    }

    public string GenerateKey(string userId, string pluginType, string pluginVersion, object? specifications)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append($"{userId}-{pluginType}-{pluginVersion}");

        if (specifications != null)
            stringBuilder.Append($"-{_jsonSerializer.Serialize(specifications)}");

        return HashKey(stringBuilder.ToString());
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