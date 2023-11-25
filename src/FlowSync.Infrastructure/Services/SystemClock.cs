using FlowSync.Core.Common.Services;

namespace FlowSync.Infrastructure.Services;

public class SystemClock : ISystemClock
{
    public DateTime NowUtc => DateTime.UtcNow;
}