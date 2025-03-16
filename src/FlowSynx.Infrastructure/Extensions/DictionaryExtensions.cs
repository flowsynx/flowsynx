using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.Extensions;

public static class DictionaryExtensions
{
    public static PluginSpecifications ToPluginSpecifications(this Dictionary<string, object?>? source)
    {
        if (source == null)
            return new PluginSpecifications();

        return new PluginSpecifications(source);
    }

    public static PluginParameters ToPluginParameters(this Dictionary<string, object?>? source)
    {
        if (source == null)
            return new PluginParameters();

        return new PluginParameters(source);
    }
}