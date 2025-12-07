using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost.PluginLoaders;

public class DirectPluginReferenceLoader(IPlugin pluginInstance) : IPluginLoader
{
    private readonly IPlugin? _pluginInstance = pluginInstance;

    public IPlugin GetPlugin() => _pluginInstance ?? throw new ObjectDisposedException(nameof(DirectPluginReferenceLoader));

    public void Load() { }

    public void Unload() { }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}