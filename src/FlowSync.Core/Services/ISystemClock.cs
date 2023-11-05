namespace FlowSync.Core.Services;

public interface ISystemClock
{
    DateTime NowUtc { get; }
}