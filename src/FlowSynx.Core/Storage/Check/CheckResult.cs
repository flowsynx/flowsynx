using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Storage.Check;

public class CheckResult
{
    public CheckResult(StorageEntity entity, CheckState state)
    {
        Entity = entity;
        State = state;
    }

    public StorageEntity Entity { get; set; }
    public CheckState State { get; set; }
}