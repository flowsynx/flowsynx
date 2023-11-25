using FlowSync.Abstractions.Entities;

namespace FlowSync.Abstractions;

public abstract class Plugin
{
    private readonly IDictionary<string, object>? _specifications;

    protected Plugin(IDictionary<string, object>? specifications)
    {
        _specifications = specifications;
    }

    public abstract Guid Id { get; }
    public abstract string Name { get; }
    public abstract string? Description { get; }
    public abstract Task<Usage> About(CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<FileSystemEntity>> ListAsync(string path, FileSystemFilterOptions fileSystemFilters, CancellationToken cancellationToken = default);
    public abstract Task WriteAsync(string path, FileStream dataStream, bool append = false, CancellationToken cancellationToken = default);
    public abstract Task<FileStream> ReadAsync(string path, CancellationToken cancellationToken = default);
    public abstract Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
    public abstract Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);
    public abstract Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default);
}