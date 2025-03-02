using FlowSynx.Core.Services;

namespace FlowSynx.Infrastructure.Services;

public class SystemClock : ISystemClock
{
    public DateTime NowUtc => DateTime.UtcNow;
}