using FlowSynx.Domain.Entities.PluignConfig;

namespace FlowSynx.Core.Extensions;
public static class PluginConfigurationSpecificationsExtensions
{
    public static PluginConfigurationSpecifications ToPluginConfigurationSpecifications(this Dictionary<string, object?>? dictionary)
    {
        if (dictionary is null)
            return new PluginConfigurationSpecifications();

        return new PluginConfigurationSpecifications(dictionary);
    }
}
