namespace FlowSynx.Domain.Entities.PluignConfig;

public class PluginConfigurationSpecifications : Dictionary<string, object?>
{
    public PluginConfigurationSpecifications(IDictionary<string, object?> dictionary) 
        : base(dictionary, StringComparer.OrdinalIgnoreCase)
    { 
    
    }

    public PluginConfigurationSpecifications() 
        : base(StringComparer.OrdinalIgnoreCase)
    {

    }
}