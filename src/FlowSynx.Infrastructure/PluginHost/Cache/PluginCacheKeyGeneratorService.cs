using FlowSynx.Application.Serialization;
using System.Text;
using FlowSynx.Infrastructure.Extensions;

namespace FlowSynx.Infrastructure.PluginHost.Cache;

public class PluginCacheKeyGeneratorService : IPluginCacheKeyGeneratorService
{
    private readonly IJsonSerializer _jsonSerializer;

    public PluginCacheKeyGeneratorService(IJsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        _jsonSerializer = serializer;
    }

    public string GenerateKey(string userId, string pluginType, string pluginVersion, object? specifications)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append($"{userId}-{pluginType}-{pluginVersion}");

        if (specifications != null)
            stringBuilder.Append($"-{_jsonSerializer.Serialize(specifications)}");

        return stringBuilder.ToString().Md5HashKey();
    }
}