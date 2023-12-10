using FlowSync.Abstractions.Common.Helpers;

namespace FlowSync.Abstractions.Storage;

public class StorageListOptions
{
    public StorageFilterItemKind Kind { get; set; } = StorageFilterItemKind.FileAndDirectory;
    public string? Sorting { get; set; }
    public int? MaxResult { get; set; }
}