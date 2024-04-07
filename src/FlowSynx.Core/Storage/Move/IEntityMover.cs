using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Storage.Move;

public interface IEntityMover
{
    Task Move(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo,
        StorageSearchOptions searchOptions, StorageMoveOptions moveOptions,
        CancellationToken cancellationToken = default);
}
