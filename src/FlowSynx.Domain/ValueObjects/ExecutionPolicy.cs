using FlowSynx.Domain.Enums;

namespace FlowSynx.Domain.ValueObjects;

public sealed record ExecutionPolicy
{
    public ExecutionStrategy Strategy { get; init; }
    public int MaxRetryAttempts { get; init; }
    public int RetryDelayMilliseconds { get; init; }
    public string? FallbackActivity { get; init; }
    public string? HealthCheckActivity { get; init; }
}