using EnsureThat;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Parers.Norms.Storage;

public class StorageNormsInfo
{
    public StorageNormsInfo(IStoragePlugin plugin, Dictionary<string, string?>? specifications, string path)
    {
        EnsureArg.IsNotNull(plugin, nameof(plugin));
        Plugin = plugin;
        Specifications = specifications;
        plugin.Specifications = Specifications;
        Path = path;
    }

    public string Path { get; }
    public IStoragePlugin Plugin { get; }
    public Dictionary<string, string?>? Specifications { get; }

    public Task Initialize()
    {
        return Plugin.Initialize();
    }
}