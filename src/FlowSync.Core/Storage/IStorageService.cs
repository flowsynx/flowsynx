using FlowSync.Abstractions.Storage;
using FlowSync.Core.Parers.Norms.Storage;
using FlowSync.Core.Storage.Options;

namespace FlowSync.Core.Storage;

internal interface IStorageService
{
    Task<StorageUsage> About(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);
    Task<IEnumerable<StorageEntity>> List(StorageNormsInfo storageNormsInfo, StorageSearchOptions searchOptions, StorageListOptions listOptions, CancellationToken cancellationToken = default);
    Task WriteAsync(StorageNormsInfo storageNormsInfo, StorageStream storageStream, CancellationToken cancellationToken = default);
    Task<StorageStream> ReadAsync(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);
    Task Delete(StorageNormsInfo storageNormsInfo, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default);
    Task DeleteFile(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);
    Task MakeDirectoryAsync(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);
    Task PurgeDirectoryAsync(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);
    Task Copy(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo, StorageSearchOptions searchOptions, StorageCopyOptions copyOptions, CancellationToken cancellationToken = default);
    Task Move(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo, StorageSearchOptions searchOptions, StorageMoveOptions moveOptions, CancellationToken cancellationToken = default);
}