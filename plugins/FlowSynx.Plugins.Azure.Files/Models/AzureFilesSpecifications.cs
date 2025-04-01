using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.Azure.Files.Models;

public class AzureFilesSpecifications : PluginSpecifications
{
    [RequiredMember]
    public string ShareName { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string AccountKey { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;
}