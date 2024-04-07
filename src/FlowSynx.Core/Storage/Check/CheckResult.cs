using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Storage.Check;

public class CheckResult
{
    public StorageEntity Entity { get; set; }
    public CheckState State { get; set; }
}