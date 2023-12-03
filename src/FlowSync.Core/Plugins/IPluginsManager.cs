using FlowSync.Abstractions;
using FlowSync.Abstractions.Storage;

namespace FlowSync.Core.Plugins;

public interface IPluginsManager
{
    IEnumerable<IPlugin> Plugins();
    IEnumerable<IPlugin> Plugins(PluginNamespace @namespace);
    IPlugin GetPlugin(string type);
    bool IsExist(string type);
}