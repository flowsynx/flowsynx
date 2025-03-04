using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.Extensions;

public static class DictionaryExtensions
{
    public static PluginSpecifications ToPluginParameters(this Dictionary<string, object?>? source)
    {
        if (source == null)
            return new PluginSpecifications();

        return new PluginSpecifications(source);
    }
}