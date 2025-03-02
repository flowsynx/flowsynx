using FlowSynx.PluginCore;

namespace FlowSynx.Connectors.Storage.LocalFileSystem.Models;

public class OperationParameter
{
    [RequiredMember]
    public string Operation { get; set; } = string.Empty;
}