using FlowSynx.Core.Storage.Models;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Storage.Models;

public class CheckResult
{
    public StorageEntity Entity { get; set; }
    public CheckState State { get; set; }
}