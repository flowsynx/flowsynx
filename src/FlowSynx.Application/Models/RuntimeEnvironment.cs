using FlowSynx.Domain.Enums;

namespace FlowSynx.Application.Models;

public class RuntimeEnvironment
{
    public string? RuntimeVersion { get; set; }                // e.g., "2.1.0"
    public string? Platform { get; set; }                       // e.g., "linux/amd64"
    public SystemInfo SystemInfo { get; set; } = new();
}

public class SystemInfo
{
    public CpuInfo? Cpu { get; set; }
    public MemoryInfo? Memory { get; set; }
    public GpuInfo? Gpu { get; set; }
    public StorageInfo? Storage { get; set; }
    public ParallelismInfo? Parallelism { get; set; }
}

public class CpuInfo
{
    public int? Cores { get; set; }
    public int? LogicalProcessors { get; set; }
    public double? ClockGHz { get; set; }
}

public class MemoryInfo
{
    public int? CapacityInGb { get; set; }
}

public class GpuInfo
{
    public int? MemoryInGb { get; set; }
    public string? Vendor { get; set; }
}

public class StorageInfo
{
    public int? FreeSpaceInGb { get; set; }
}

public class ParallelismInfo
{
    public int? Threads { get; set; }
    public int? ConcurrentProcesses { get; set; }
    public bool? RequiresGpuParallelism { get; set; }
}