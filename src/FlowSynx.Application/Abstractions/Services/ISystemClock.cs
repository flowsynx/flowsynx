namespace FlowSynx.Application.Abstractions.Services;

public interface ISystemClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
}