namespace FlowSynx.Core.Storage.Models;

public enum CheckState
{
    Error = 0,
    Match = 1,
    Different = 2,
    MissedOnDestination = 4,
    MissedOnSource = 8,
}