using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost.PluginLoaders;

public interface IPluginLoader: IDisposable
{
    IPlugin Plugin { get; }
    void Load();
    void Unload();
}