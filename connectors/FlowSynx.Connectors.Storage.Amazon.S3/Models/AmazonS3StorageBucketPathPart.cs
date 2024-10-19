namespace FlowSynx.Connectors.Storage.Amazon.S3.Models;

internal class AmazonS3StorageBucketPathPart
{
    public AmazonS3StorageBucketPathPart(string bucketName, string relativePath)
    {
        BucketName = bucketName;
        RelativePath = relativePath;
    }

    public string BucketName { get; }
    public string RelativePath { get; }
}