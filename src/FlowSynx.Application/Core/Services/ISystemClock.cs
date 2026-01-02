namespace FlowSynx.Application.Core.Services;

public interface ISystemClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
}