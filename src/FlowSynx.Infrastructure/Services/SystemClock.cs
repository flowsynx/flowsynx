using FlowSynx.Application.Services;

namespace FlowSynx.Infrastructure.Services;

public class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
}