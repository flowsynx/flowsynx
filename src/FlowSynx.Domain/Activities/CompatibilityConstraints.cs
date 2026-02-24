using FlowSynx.Domain.Enums;

namespace FlowSynx.Domain.Activities;

public class CompatibilityConstraints
{
    public CpuConstraint? Cpu { get; set; }
    public MemoryConstraint? Memory { get; set; }
    public GpuConstraint? Gpu { get; set; }
    public StorageConstraint? Storage { get; set; }
    public ParallelismConstraint? Parallelism { get; set; }
}

public class CpuConstraint
{
    public int? MinCores { get; set; }
    public int? MinLogicalProcessors { get; set; }
    public double? MinClockGHz { get; set; }
}

public class MemoryConstraint
{
    public int? MinGb { get; set; }
}

public class GpuConstraint
{
    public int? MinMemoryGb { get; set; }
    public GpuVendor? Vendor { get; set; }
}

public class StorageConstraint
{
    public int? MinFreeSpaceGb { get; set; }
    public StorageType? Type { get; set; }
    public int? MinReadSpeedMbps { get; set; }
}

public class ParallelismConstraint
{
    public int? MinThreads { get; set; }
    public int? MinConcurrentProcesses { get; set; }
    public bool? RequiresGpuParallelism { get; set; }
}