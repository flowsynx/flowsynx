using FlowSync.Abstractions;
using FlowSync.Core.Exceptions;

namespace FlowSync.Core.Plugins;

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
                throw new PluginLoadingException(string.Format(FlowSyncCoreResource.PlugininItializingInstanceErrorMessage, Name));

            return (Plugin)instance;
        }
        catch (Exception ex)
        {
            throw new PluginLoadingException(ex.Message);
        }
    }
}