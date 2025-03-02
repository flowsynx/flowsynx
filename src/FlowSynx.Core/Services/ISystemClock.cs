namespace FlowSynx.Core.Services;

public interface ISystemClock
{
    DateTime NowUtc { get; }
}