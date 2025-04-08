using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost;

public interface IPluginLoader
{
    PluginHandle LoadPlugin(string pluginLocation);
}