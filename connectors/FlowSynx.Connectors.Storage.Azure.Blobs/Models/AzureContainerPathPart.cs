namespace FlowSynx.Connectors.Storage.Azure.Blobs.Models;

internal class AzureBlobEntityPart
{
    public AzureBlobEntityPart(string containerName, string relativePath)
    {
        ContainerName = containerName;
        RelativePath = relativePath;
    }

    public string ContainerName { get; }
    public string RelativePath { get; }
}