namespace FlowSynx.Connectors.Storage.Google.Cloud.Models;

internal class GoogleCloudPathPart
{
    public GoogleCloudPathPart(string bucketName, string relativePath)
    {
        BucketName = bucketName;
        RelativePath = relativePath;
    }

    public string BucketName { get; }
    public string RelativePath { get; }
}