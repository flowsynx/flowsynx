using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginHandle
{
    public PluginLoadContext LoadContext { get; }
    public IPlugin Instance { get; }

    public PluginHandle(PluginLoadContext loadContext, IPlugin pluginInstance)
    {
        LoadContext = loadContext;
        Instance = pluginInstance;
    }

    //public void Execute() => Instance.ExecuteAsync();

    public void Unload()
    {
        LoadContext.Unload();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
