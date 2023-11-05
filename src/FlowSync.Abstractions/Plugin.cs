using FlowSync.Abstractions.Entities;

namespace FlowSync.Abstractions;

public abstract class Plugin
{
    private readonly IDictionary<string, object>? _specifications;

    protected Plugin(IDictionary<string, object>? specifications)
    {
        _specifications = specifications;
    }

    public virtual Guid Id { get; } = Guid.NewGuid();
    public abstract string Name { get; }
    public abstract string? Description { get; }
    public abstract IEnumerable<string>? SupportedVersions { get; }
    public abstract Task<IEnumerable<Entity>> ListAsync(string path, FilterOptions filters, CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<bool>> ExistsAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default);
    public abstract Task WriteAsync(string path, Stream dataStream, bool append = false, CancellationToken cancellationToken = default);
    public abstract Task<Stream> ReadAsync(string path, CancellationToken cancellationToken = default);
    public abstract Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);
    public abstract Task DeleteAsync(IEnumerable<string> path, CancellationToken cancellationToken = default);
}