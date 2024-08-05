namespace FlowSynx.Plugin.Storage.Memory;

public class MemoryEntity
{
    public byte[] Content { get; set; }

    public MemoryEntity(): this(Array.Empty<byte>())
    {
    }

    public MemoryEntity(byte[] content)
    {
        Content = content;
    }
}