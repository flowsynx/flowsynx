using FlowSynx.Application.Models;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginLoader : IPluginLoader
{
    public PluginHandle LoadPlugin(string pluginLocation)
    {
        var context = new PluginLoadContext(pluginLocation);
        var assembly = context.LoadFromAssemblyPath(pluginLocation);

        var pluginType = assembly.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);
        if (pluginType is null)
            throw new FlowSynxException((int)ErrorCode.PluginNotFound, "The plugin not found.");

        var instance = (IPlugin)Activator.CreateInstance(pluginType)!;
        return new PluginHandle(context, instance); ;
    }
}