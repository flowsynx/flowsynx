namespace FlowSynx.Plugin.Storage.Azure.Blobs;

internal class AzureContainerPathPart
{
    public AzureContainerPathPart(string containerName, string relativePath)
    {
        ContainerName = containerName;
        RelativePath = relativePath;
    }

    public string ContainerName { get; }
    public string RelativePath { get; }
}