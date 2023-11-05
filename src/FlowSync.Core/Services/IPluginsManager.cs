using FlowSync.Core.Utilities.Plugins;

namespace FlowSync.Core.Services;

public interface IPluginsManager
{
    PluginItem GetPlugin(string name);
    IEnumerable<PluginItem> Plugins();
    bool IsExist(string name);
}