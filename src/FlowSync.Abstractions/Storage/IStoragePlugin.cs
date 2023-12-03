namespace FlowSync.Abstractions.Storage;

public interface IStoragePlugin : IPlugin, IDisposable
{
    Specifications? Specifications { get; set; }
    Task<StorageUsage> About(CancellationToken cancellationToken = default);
    Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions, int? maxResult, CancellationToken cancellationToken = default);
    Task WriteAsync(string path, StorageStream storageStream, bool append = false, CancellationToken cancellationToken = default);
    Task<StorageStream> ReadAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
    Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default);
}