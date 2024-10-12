namespace FlowSynx.Connectors.Storage.Google.Cloud;

internal class GoogleCloudStorageBucketPathPart
{
    public GoogleCloudStorageBucketPathPart(string bucketName, string relativePath)
    {
        BucketName = bucketName;
        RelativePath = relativePath;
    }

    public string BucketName { get; }
    public string RelativePath { get; }
}