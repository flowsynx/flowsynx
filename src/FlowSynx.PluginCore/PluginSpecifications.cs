namespace FlowSynx.PluginCore;

public class PluginSpecifications : Dictionary<string, string?>, ICloneable
{
    public PluginSpecifications() : base(StringComparer.OrdinalIgnoreCase)
    {

    }

    public object Clone()
    {
        var clone = (PluginSpecifications)MemberwiseClone();
        return clone;
    }
}