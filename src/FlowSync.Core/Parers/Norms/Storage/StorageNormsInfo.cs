using EnsureThat;
using FlowSync.Abstractions;
using FlowSync.Abstractions.Storage;

namespace FlowSync.Core.Parers.Norms.Storage;

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