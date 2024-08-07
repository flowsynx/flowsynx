namespace FlowSynx.Plugin.Storage.Memory;

internal class MemoryStorageBucketPathPart
{
    public MemoryStorageBucketPathPart()
        : this(string.Empty, string.Empty)
    {

    }

    public MemoryStorageBucketPathPart(string bucketName)
        : this(bucketName, string.Empty)
    {

    }

    public MemoryStorageBucketPathPart(string bucketName, string relativePath)
    {
        BucketName = bucketName;
        RelativePath = relativePath;
    }

    public string BucketName { get; }
    public string RelativePath { get; }
}