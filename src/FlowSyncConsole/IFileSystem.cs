namespace FlowSyncConsole;

public interface IFileSystem : IDisposable
{
    ICollection<FileSystemPath> List(FileSystemPath path);
    bool Exists(FileSystemPath path);
    Stream CreateFile(FileSystemPath path);
    Stream OpenFile(FileSystemPath path, FileAccess access);
    void DeleteFile(FileSystemPath path);
    void MakeDirectory(FileSystemPath path);
    void DeleteDirectory(FileSystemPath path);
}