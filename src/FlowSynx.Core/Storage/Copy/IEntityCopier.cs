using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Storage.Copy;

public interface IEntityCopier
{
    Task Copy(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo,
        StorageSearchOptions searchOptions, StorageCopyOptions copyOptions,
        CancellationToken cancellationToken = default);
}
