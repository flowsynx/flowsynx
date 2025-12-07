using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.Extensions;

public static class DictionaryExtensions
{
    public static PluginParameters ToPluginParameters(this Dictionary<string, object?>? source)
    {
        return source == null ? new PluginParameters() : new PluginParameters(source);
    }
}