using FlowSync.Abstractions;
using FlowSync.Core.Exceptions;

namespace FlowSync.Core.Utilities.Plugins;

public class PluginItem
{
    public required string Name { get; set; }
    public required Type Type { get; set; }

    public Plugin NewInstance(IDictionary<string, object>? specifications)
    {
        try
        {
            var instance = Activator.CreateInstance(Type, specifications);
            if (instance == null)
                throw new PluginLoadingException($"Error in initializing '{Name}' plugin instance!");
            
            return (Plugin)instance;
        }
        catch (Exception ex)
        {
            throw new PluginLoadingException(ex.Message);
        }
    }
}