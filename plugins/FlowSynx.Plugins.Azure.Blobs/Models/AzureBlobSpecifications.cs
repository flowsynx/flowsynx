using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.Azure.Blobs.Models;

public class AzureBlobSpecifications : PluginSpecifications
{
    [RequiredMember]
    public string AccountName { get; set; } = string.Empty;

    [RequiredMember]
    public string AccountKey { get; set; } = string.Empty;

    [RequiredMember]
    public string ContainerName { get; set; } = string.Empty;
}