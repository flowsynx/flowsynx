namespace FlowSynx.Connectors.Storage.Memory.Models;

public class MemoryEntity : StorageEntity
{
    public byte[]? Content { get; }

    public MemoryEntity(string fullPath) : base(fullPath, StorageEntityItemKind.Directory)
    {
        Content = null;
        Size = 0;
        ModifiedTime = DateTimeOffset.Now;
    }

    public MemoryEntity(string fullPath, byte[] content) : base(fullPath, StorageEntityItemKind.File)
    {
        Content = content;
        Size = content.Length;
        ModifiedTime = DateTimeOffset.Now;
    }

    public MemoryEntity(string folderPath, string name) : base(folderPath, name, StorageEntityItemKind.Directory)
    {
        Content = null;
        Size = 0;
        ModifiedTime = DateTimeOffset.Now;
    }

    public MemoryEntity(string folderPath, string name, byte[] content) : base(folderPath, name, StorageEntityItemKind.File)
    {
        Content = content;
        Size = content.Length;
        ModifiedTime = DateTimeOffset.Now;
    }
}