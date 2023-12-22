using EnsureThat;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Parers.Norms.Storage;

public class StorageNormsInfo
{
    public StorageNormsInfo(IStoragePlugin plugin, Specifications? specifications, string path)
    {
        EnsureArg.IsNotNull(plugin, nameof(plugin));
        Plugin = plugin;
        Specifications = specifications;
        plugin.Specifications = Specifications;
        Path = path;
    }

    public string Path { get; }
    public IStoragePlugin Plugin { get; }
    public Specifications? Specifications { get; }
}