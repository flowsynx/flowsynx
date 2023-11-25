namespace FlowSync.Core.Plugins;

public interface IPluginsManager
{
    PluginItem GetPlugin(string name);
    IEnumerable<PluginItem> Plugins();
    bool IsExist(string name);
}