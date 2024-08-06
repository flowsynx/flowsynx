using FlowSynx.Security;

namespace FlowSynx.Plugin.Storage.Memory;

public class MemoryEntity: StorageEntity
{
    public Stream Content { get; }

    public MemoryEntity(string fullPath) : base(fullPath, StorageEntityItemKind.Directory)
    {
        Content = Stream.Null;
        Size = 0;
        ModifiedTime = DateTimeOffset.Now;
    }

    public MemoryEntity(string fullPath, Stream content) : base(fullPath, StorageEntityItemKind.File)
    {
        Content = content;
        Size = content.Length;
        Md5 = HashHelper.Md5.GetHash(content);
        ModifiedTime = DateTimeOffset.Now;
    }

    public MemoryEntity(string folderPath, string name) : base(folderPath, name, StorageEntityItemKind.Directory)
    {
        Content = Stream.Null;
        Size = 0;
        ModifiedTime = DateTimeOffset.Now;
    }

    public MemoryEntity(string folderPath, string name, Stream content) : base(folderPath, name, StorageEntityItemKind.File)
    {
        Content = content;
        Size = content.Length;
        Md5 = HashHelper.Md5.GetHash(content);
        ModifiedTime = DateTimeOffset.Now;
    }
}