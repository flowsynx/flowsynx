namespace FlowSynx.Plugin.Storage.Memory;

public class MemoryEntity
{
    public Stream Content { get; set; }

    public MemoryEntity(Stream content)
    {
        Content = content;
    }
}