using System.Collections.Concurrent;

namespace FlowSynx.PluginCore;

public class PluginParameters : Dictionary<string, object?>, ICloneable
{
    public PluginParameters(IDictionary<string, object?> dictionary)
    : base(dictionary, StringComparer.OrdinalIgnoreCase)
    {

    }

    public PluginParameters()
        : base(StringComparer.OrdinalIgnoreCase)
    {

    }

    public object Clone()
    {
        var clone = (PluginParameters)MemberwiseClone();
        return clone;
    }
}