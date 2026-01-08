namespace FlowSynx.Domain.Chromosomes;

public record ResourceConstraints(
    int MaxMemoryMB = 100,
    int MaxCpuPercent = 50,
    int TimeoutMs = 5000,
    int MaxConcurrent = 5,
    Dictionary<string, object> ResourceLimits = null);