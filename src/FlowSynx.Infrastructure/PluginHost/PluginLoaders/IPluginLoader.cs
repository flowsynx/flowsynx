using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost.PluginLoaders;

public interface IPluginLoader: IDisposable
{
    IPlugin GetPlugin();
    void Load();
    void Unload();
}