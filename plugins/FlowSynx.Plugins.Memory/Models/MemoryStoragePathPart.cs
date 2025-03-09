namespace FlowSynx.Connectors.Storage.Memory.Models;

internal class MemoryStoragePathPart
{
    public MemoryStoragePathPart()
        : this(string.Empty, string.Empty)
    {

    }

    public MemoryStoragePathPart(string bucketName)
        : this(bucketName, string.Empty)
    {

    }

    public MemoryStoragePathPart(string bucketName, string relativePath)
    {
        BucketName = bucketName;
        RelativePath = relativePath;
    }

    public string BucketName { get; }
    public string RelativePath { get; }
}