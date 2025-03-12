namespace FlowSynx.Application.Services;

public interface ISystemClock
{
    DateTime NowUtc { get; }
}