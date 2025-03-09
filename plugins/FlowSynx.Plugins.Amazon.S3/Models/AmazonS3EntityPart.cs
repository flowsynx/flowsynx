namespace FlowSynx.Connectors.Storage.Amazon.S3.Models;

internal class AmazonS3EntityPart
{
    public AmazonS3EntityPart(string bucketName, string relativePath)
    {
        BucketName = bucketName;
        RelativePath = relativePath;
    }

    public string BucketName { get; }
    public string RelativePath { get; }
}