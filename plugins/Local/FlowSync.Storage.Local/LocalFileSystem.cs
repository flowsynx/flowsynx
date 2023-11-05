using FlowSync.Abstractions;
using FlowSync.Abstractions.Entities;
using FlowSync.Storage.Local.Extensions;

namespace FlowSync.Storage.Local;

public class LocalFileSystem : Plugin
{
    private readonly IDictionary<string, object>? _specifications;

    public LocalFileSystem(IDictionary<string, object>? specifications) : base(specifications)
    {
        _specifications = specifications;
    }

    public override Guid Id => Guid.Parse("f6304870-0294-453e-9598-a82167ace653");
    public override string Name => "Local";
    public override string? Description => null;
    public override IEnumerable<string>? SupportedVersions => new List<string>() { "1.0", "2.0" };

    public override Task<IEnumerable<Entity>> ListAsync(string path, FilterOptions filters, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path), "The path should not be empty!");

            if (IsWindows)
                path = path.ToWindowsPath();

            if (!Directory.Exists(path))
                throw new Exception("The given path is not exist. Please correct it and try again!");

            var result = new List<Entity>();
            var searchOption = filters.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var directoryInfo = new DirectoryInfo(path);

            if (filters.Kind is FilterItemKind.File or FilterItemKind.FileAndDirectory)
                result.AddRange(directoryInfo.EnumerateFiles("*", searchOption).Select(GetVirtualFilePath));

            if (filters.Kind is FilterItemKind.Directory or FilterItemKind.FileAndDirectory)
                result.AddRange(directoryInfo.EnumerateDirectories("*", searchOption).Select(GetVirtualDirectoryPath));

            return Task.FromResult(result.AsEnumerable());
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public override Task<IEnumerable<bool>> ExistsAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task WriteAsync(string path, Stream dataStream, bool append = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<Stream> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteAsync(IEnumerable<string> path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #region internal methods
    private bool IsWindows => OperatingSystem.IsWindows();

    private Entity GetVirtualDirectoryPath(DirectoryInfo directory)
    {
        return new Entity(directory.FullName.ToUnixPath(), EntityItemKind.Directory)
        {
            CreatedTime = directory.CreationTime,
            LastModificationTime = directory.LastWriteTime,
            Size = null,
            Properties = { }
        };
    }

    private Entity GetVirtualFilePath(FileInfo file)
    {
        return new Entity(file.FullName.ToUnixPath(), EntityItemKind.File)
        {
            CreatedTime = file.CreationTime,
            LastModificationTime = file.LastWriteTime,
            Size = file.Length
        };
    }
    #endregion
}