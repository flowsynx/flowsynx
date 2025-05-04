using FlowSynx.Domain.PluginConfig;

namespace FlowSynx.Application.Extensions;

public static class PluginConfigurationSpecificationsExtensions
{
    public static PluginConfigurationSpecifications ToPluginConfigurationSpecifications(this Dictionary<string, object?>? dictionary)
    {
        return dictionary is null 
            ? new PluginConfigurationSpecifications() 
            : new PluginConfigurationSpecifications(dictionary);
    }
}
