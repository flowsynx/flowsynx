namespace FlowSynx.BuildingBlocks.Clock;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
}