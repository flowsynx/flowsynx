using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowSyncConsole;

public class PhysicalFileSystem : IFileSystem
{
    #region Internals
    public string PhysicalRoot { get; private set; }

    public PhysicalFileSystem(string physicalRoot)
    {
        if (!Path.IsPathRooted(physicalRoot))
            physicalRoot = Path.GetFullPath(physicalRoot);
        if (physicalRoot[^1] != Path.DirectorySeparatorChar)
            physicalRoot = physicalRoot + Path.DirectorySeparatorChar;
        PhysicalRoot = physicalRoot;
    }

    public string GetPhysicalPath(FileSystemPath path)
    {
        return Path.Combine(PhysicalRoot, path.ToString().Remove(0, 1).Replace(FileSystemPath.DirectorySeparator, Path.DirectorySeparatorChar));
    }

    public FileSystemPath GetVirtualFilePath(string physicalPath)
    {
        if (!physicalPath.StartsWith(PhysicalRoot, StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException("The specified path is not member of the PhysicalRoot.", nameof(physicalPath));
        var virtualPath = FileSystemPath.DirectorySeparator + physicalPath.Remove(0, PhysicalRoot.Length).Replace(Path.DirectorySeparatorChar, FileSystemPath.DirectorySeparator);
        return FileSystemPath.Parse(virtualPath);
    }

    public FileSystemPath GetVirtualDirectoryPath(string physicalPath)
    {
        if (!physicalPath.StartsWith(PhysicalRoot, StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException("The specified path is not member of the PhysicalRoot.", nameof(physicalPath));
        var virtualPath = FileSystemPath.DirectorySeparator + physicalPath.Remove(0, PhysicalRoot.Length).Replace(Path.DirectorySeparatorChar, FileSystemPath.DirectorySeparator);
        if (virtualPath[^1] != FileSystemPath.DirectorySeparator)
            virtualPath += FileSystemPath.DirectorySeparator;
        return FileSystemPath.Parse(virtualPath);
    }

    #endregion

    public ICollection<FileSystemPath> GetEntities(FileSystemPath path)
    {
        var physicalPath = GetPhysicalPath(path);
        var directories = System.IO.Directory.GetDirectories(physicalPath);
        var files = System.IO.Directory.GetFiles(physicalPath);
        var virtualDirectories =
            directories.Select(GetVirtualDirectoryPath);
        var virtualFiles =
            files.Select(GetVirtualFilePath);
        return new EnumerableCollection<FileSystemPath>(virtualDirectories.Concat(virtualFiles), directories.Length + files.Length);
    }

    public bool Exists(FileSystemPath path)
    {
        return path.IsFile ? System.IO.File.Exists(GetPhysicalPath(path)) : System.IO.Directory.Exists(GetPhysicalPath(path));
    }

    public Stream CreateFile(FileSystemPath path)
    {
        if (!path.IsFile)
            throw new ArgumentException("The specified path is not a file.", nameof(path));
        return System.IO.File.Create(GetPhysicalPath(path));
    }

    public Stream OpenFile(FileSystemPath path, FileAccess access)
    {
        if (!path.IsFile)
            throw new ArgumentException("The specified path is not a file.", nameof(path));
        return System.IO.File.Open(GetPhysicalPath(path), FileMode.Open, access);
    }

    public void CreateDirectory(FileSystemPath path)
    {
        if (!path.IsDirectory)
            throw new ArgumentException("The specified path is not a directory.", nameof(path));
        System.IO.Directory.CreateDirectory(GetPhysicalPath(path));
    }

    public void Delete(FileSystemPath path)
    {
        if (path.IsFile)
            System.IO.File.Delete(GetPhysicalPath(path));
        else
            System.IO.Directory.Delete(GetPhysicalPath(path), true);
    }

    public void Dispose()
    {
    }
}