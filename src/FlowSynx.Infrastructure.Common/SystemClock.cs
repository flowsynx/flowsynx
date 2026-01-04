using FlowSynx.Application.Abstractions.Services;

namespace FlowSynx.Infrastructure.Common;

public class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
}