using FlowSynx.Application.Services;

namespace FlowSynx.Infrastructure.Services;

public class SystemClock : ISystemClock
{
    public DateTime NowUtc => DateTime.UtcNow;
}