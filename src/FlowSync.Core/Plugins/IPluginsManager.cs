using FlowSync.Abstractions;

namespace FlowSync.Core.Plugins;

public interface IPluginsManager
{
    IFileSystemPlugin GetPlugin(string type);
    IEnumerable<IFileSystemPlugin> Plugins();
    bool IsExist(string type);
}