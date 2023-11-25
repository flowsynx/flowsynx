namespace FlowSync.Core.Common.Services;

public interface ISystemClock
{
    DateTime NowUtc { get; }
}