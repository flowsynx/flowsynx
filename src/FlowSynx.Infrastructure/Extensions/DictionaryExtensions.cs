using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.Extensions;

public static class DictionaryExtensions
{
    public static PluginSpecifications ToPluginParameters(this Dictionary<string, string?>? source)
    {
        var specifications = new PluginSpecifications();
        if (source is null) 
            return specifications;

        foreach (var item in source)
        {
            specifications.Add(item.Key, item.Value);
        }
        return specifications;
    }
}