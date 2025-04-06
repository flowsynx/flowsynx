using FlowSynx.PluginCore;

namespace FlowSynx.Application.PluginHost;

public interface IPluginLoader
{
    Task<IPlugin> LoadPlugin(string pluginLocation, CancellationToken cancellationToken);
}