namespace FlowSynx.BuildingBlocks.Clock;

public interface IClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
}