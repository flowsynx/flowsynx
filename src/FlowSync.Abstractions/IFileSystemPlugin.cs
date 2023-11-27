using FlowSync.Abstractions.Entities;
using FlowSync.Abstractions.Models;

namespace FlowSync.Abstractions;

public interface IFileSystemPlugin
{
    Guid Id { get; }
    string Namespace { get; }
    string? Description { get; }
    void SetSpecifications(IDictionary<string, object>? specifications);
    Task<Usage> About(CancellationToken cancellationToken = default);
    Task<IEnumerable<FileSystemEntity>> ListAsync(string path, FileSystemFilterOptions fileSystemFilters, CancellationToken cancellationToken = default);
    Task WriteAsync(string path, FileStream dataStream, bool append = false, CancellationToken cancellationToken = default);
    Task<FileStream> ReadAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, FileSystemFilterOptions fileSystemFilters, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
    Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default);
}