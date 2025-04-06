using FlowSynx.Application.PluginHost;
using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginLoader : IPluginLoader
{
    public Task<IPlugin> LoadPlugin(string pluginLocation, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}