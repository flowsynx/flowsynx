using FlowSync.Abstractions;
using FlowSync.Abstractions.Storage;

namespace FlowSync.Core.Parers.Norms.Storage;

public class StorageNormsInfo
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required IStoragePlugin Plugin { get; set; }
    public required Specifications? Specifications { get; set; }
}