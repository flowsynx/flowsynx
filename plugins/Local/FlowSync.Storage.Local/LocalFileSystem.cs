using FlowSync.Abstractions;
using FlowSync.Abstractions.Entities;
using FlowSync.Storage.Local.Extensions;
using System.IO;

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
    
    public override Task<Usage> About(CancellationToken cancellationToken = default)
    {
        long totalSpace = 0, freeSpace = 0;
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d is not { DriveType: DriveType.Fixed, IsReady: true }) continue;

                totalSpace += d.TotalSize;
                freeSpace += d.TotalFreeSpace;
            }
        }
        catch (Exception)
        {
            totalSpace = 0;
            freeSpace = 0;
        }

        return Task.FromResult(new Usage { Total = totalSpace, Free = freeSpace});
    }

    public override Task<IEnumerable<FileSystemEntity>> ListAsync(string path, FileSystemFilterOptions fileSystemFilters, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path), "The specified path should not be empty!");

            path = GetPhysicalPath(path);

            if (!Directory.Exists(path))
                throw new ArgumentException($"The specified path '{path}' is not exist.", nameof(path));

            var result = new List<FileSystemEntity>();
            var searchOption = fileSystemFilters.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var directoryInfo = new DirectoryInfo(path);

            if (fileSystemFilters.Kind is FilterItemKind.File or FilterItemKind.FileAndDirectory)
                result.AddRange(directoryInfo.EnumerateFiles("*", searchOption).Select(GetVirtualFilePath));

            if (fileSystemFilters.Kind is FilterItemKind.Directory or FilterItemKind.FileAndDirectory)
                result.AddRange(directoryInfo.EnumerateDirectories("*", searchOption).Select(GetVirtualDirectoryPath));

            return Task.FromResult(result.AsEnumerable());
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public override Task WriteAsync(string path, FileStream dataStream, bool append = false, CancellationToken cancellationToken = default)
    {
        if (dataStream.Length == 0)
            throw new ArgumentException($"The stream data should not be null.", nameof(dataStream));

        path = GetPhysicalPath(path);
        using var fileStream = File.Create(path);
        dataStream.CopyTo(fileStream);
        return Task.CompletedTask;
    }

    public override Task<FileStream> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        path = GetPhysicalPath(path);

        if (!File.Exists(path))
            throw new ArgumentException($"The specified path '{path}' is not a file.", nameof(path));
        
        return Task.FromResult(File.OpenRead(path));
    }

    public override Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        path = GetPhysicalPath(path);

        if (!File.Exists(path))
            throw new ArgumentException($"The specified path '{path}' is not a file.", nameof(path));

        File.Delete(path);
        return Task.CompletedTask;
    }

    public override Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        path = GetPhysicalPath(path);

        if (Directory.Exists(path))
            throw new ArgumentException("The specified path is already exist.", nameof(path));

        System.IO.Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public override Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #region internal methods
    private string GetPhysicalPath(string path) => IsWindows ? path.ToWindowsPath() : path;

    private bool IsWindows => OperatingSystem.IsWindows();

    private FileSystemEntity GetVirtualDirectoryPath(DirectoryInfo directory)
    {
        return new FileSystemEntity(directory.FullName.ToUnixPath(), EntityItemKind.Directory)
        {
            CreatedTime = directory.CreationTime,
            ModifiedTime = directory.LastWriteTime,
            Size = null,
            Properties = { }
        };
    }

    private FileSystemEntity GetVirtualFilePath(FileInfo file)
    {
        //FileAttributes attributes = File.GetAttributes(path);

        return new FileSystemEntity(file.FullName.ToUnixPath(), EntityItemKind.File)
        {
            CreatedTime = file.CreationTimeUtc,
            ModifiedTime = file.LastWriteTimeUtc,
            Size = file.Length,
            //Properties = file.Attributes
        };
    }
    #endregion
}