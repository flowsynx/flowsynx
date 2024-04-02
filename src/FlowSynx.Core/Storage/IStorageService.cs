using FlowSynx.Core.Features.Storage.Check.Command;
using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Core.Storage.Models;
using FlowSynx.Core.Storage.Options;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Storage;

internal interface IStorageService
{
    Task<StorageUsage> About(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);

    Task<IEnumerable<StorageEntity>> List(StorageNormsInfo storageNormsInfo, StorageSearchOptions searchOptions, 
        StorageListOptions listOptions, StorageHashOptions hashOptions, CancellationToken cancellationToken = default);

    Task WriteAsync(StorageNormsInfo storageNormsInfo, StorageStream storageStream, StorageWriteOptions writeOptions, 
        CancellationToken cancellationToken = default);

    Task<StorageRead> ReadAsync(StorageNormsInfo storageNormsInfo, StorageHashOptions hashOptions, 
        CancellationToken cancellationToken = default);

    Task Delete(StorageNormsInfo storageNormsInfo, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default);

    Task DeleteFile(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);

    Task<bool> FileExist(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);

    Task MakeDirectoryAsync(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);

    Task PurgeDirectoryAsync(StorageNormsInfo storageNormsInfo, CancellationToken cancellationToken = default);

    Task Copy(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo, 
        StorageSearchOptions searchOptions, StorageCopyOptions copyOptions, CancellationToken cancellationToken = default);

    Task Move(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo, 
        StorageSearchOptions searchOptions, StorageMoveOptions moveOptions, CancellationToken cancellationToken = default);

    Task<IEnumerable<CheckResult>> Check(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo,
        StorageSearchOptions searchOptions, StorageCheckOptions checkOptions, CancellationToken cancellationToken = default);
}