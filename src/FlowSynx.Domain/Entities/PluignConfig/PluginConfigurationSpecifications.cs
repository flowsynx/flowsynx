namespace FlowSynx.Domain.Entities.PluignConfig;

public class PluginConfigurationSpecifications : Dictionary<string, string?>
{
    public PluginConfigurationSpecifications() : base(StringComparer.OrdinalIgnoreCase)
    {

    }
}