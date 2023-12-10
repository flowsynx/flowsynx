namespace FlowSync.Abstractions.Storage;

public interface IStoragePlugin : IPlugin, IDisposable
{
    Specifications? Specifications { get; set; }
    Task<StorageUsage> About(CancellationToken cancellationToken = default);
    Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions, StorageListOptions listOptions, CancellationToken cancellationToken = default);
    Task WriteAsync(string path, StorageStream storageStream, CancellationToken cancellationToken = default);
    Task<StorageStream> ReadAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
    Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default);
    Task PurgeDirectoryAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> DirectoryExistAsync(string path, CancellationToken cancellationToken = default);
}