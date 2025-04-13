using FlowSynx.PluginCore;
using System.Reflection;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginLoader : IPluginLoader
{
    public PluginHandle LoadPlugin(string pluginLocation)
    {
        return GetImplementationsOfInterface(pluginLocation);
    }

    private PluginHandle GetImplementationsOfInterface(string pluginLocation)
    {
        try
        {
            if (!File.Exists(pluginLocation))
                return PluginHandle.Fail($"Plugin file not found: {pluginLocation}");

            var interfaceType = typeof(IPlugin);
            var pluginAssembly = Assembly.LoadFrom(pluginLocation);
            var types = pluginAssembly.GetTypes();

            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract)
                    continue;

                if (interfaceType.IsAssignableFrom(type))
                {
                    var instance = (IPlugin)Activator.CreateInstance(type)!;
                    return PluginHandle.Ok(instance, pluginAssembly.Location);
                }
            }

            return PluginHandle.Fail("No plugin type found.");
        }
        catch (Exception ex)
        {
            return PluginHandle.Fail($"Failed to load plugin: {ex.Message}");
        }
    }
}