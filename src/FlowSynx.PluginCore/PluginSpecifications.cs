namespace FlowSynx.PluginCore;

public class PluginSpecifications : Dictionary<string, object?>, ICloneable
{
    public PluginSpecifications(IDictionary<string, object?> dictionary)
    : base(dictionary, StringComparer.OrdinalIgnoreCase)
    {

    }

    public PluginSpecifications()
        : base(StringComparer.OrdinalIgnoreCase)
    {

    }

    public object Clone()
    {
        var clone = (PluginSpecifications)MemberwiseClone();
        return clone;
    }
}