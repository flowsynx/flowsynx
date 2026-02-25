using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Enums;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public sealed class ActivityCompatibilityService : IActivityCompatibilityService
{
    public bool IsCompatible(Activity activity, RuntimeEnvironment env, out List<string> issues)
    {
        issues = new List<string>();

        if (activity is null)
        {
            issues.Add("Activity is null.");
            return false;
        }

        if (env is null)
        {
            issues.Add("RuntimeEnvironment is null.");
            return false;
        }

        var matrix = activity.Specification?.Compatibility;
        if (matrix is null)
            return true; // no compatibility metadata => no restrictions

        ValidateMinRuntimeVersion(matrix.MinRuntimeVersion, env.RuntimeVersion, issues);
        ValidatePlatforms(matrix.Platforms, env.Platform, issues);
        ValidateConstraints(matrix.Constraints, env.SystemInfo, issues);

        return issues.Count == 0;
    }

    private static void ValidateMinRuntimeVersion(string min, string? actual, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(min))
            return;

        if (string.IsNullOrWhiteSpace(actual))
        {
            issues.Add($"Compatibility.MinRuntimeVersion requires '{min}', but RuntimeEnvironment.RuntimeVersion is missing.");
            return;
        }

        if (!Version.TryParse(min.Trim(), out var minV))
        {
            issues.Add($"Compatibility.MinRuntimeVersion '{min}' is not a valid version.");
            return;
        }

        if (!Version.TryParse(actual.Trim(), out var actualV))
        {
            issues.Add($"RuntimeEnvironment.RuntimeVersion '{actual}' is not a valid version.");
            return;
        }

        if (actualV < minV)
            issues.Add($"Runtime version '{actualV}' is less than required minimum '{minV}'.");
    }

    private static void ValidatePlatforms(IReadOnlyList<string>? allowedPlatforms, string? actualPlatform, List<string> issues)
    {
        if (allowedPlatforms is null || allowedPlatforms.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(actualPlatform))
        {
            issues.Add($"Compatibility.Platforms requires one of [{string.Join(", ", allowedPlatforms)}], but RuntimeEnvironment.Platform is missing.");
            return;
        }

        var actual = actualPlatform.Trim();
        var ok = allowedPlatforms.Any(p => PlatformMatches(p, actual));
        if (!ok)
            issues.Add($"Platform '{actual}' is not compatible. Allowed: [{string.Join(", ", allowedPlatforms)}].");
    }

    private static bool PlatformMatches(string allowed, string actual)
    {
        if (string.IsNullOrWhiteSpace(allowed))
            return false;

        allowed = allowed.Trim();

        // Support simple wildcard suffix like "linux/*"
        if (allowed.EndsWith("/*", StringComparison.Ordinal))
        {
            var prefix = allowed[..^1]; // keep trailing '/'
            return actual.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(allowed, actual, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateConstraints(CompatibilityConstraints? constraints, SystemInfo sys, List<string> issues)
    {
        if (constraints is null)
            return;

        // CPU
        if (constraints.Cpu is not null)
        {
            if (constraints.Cpu.MinCores is not null)
            {
                var actual = sys.Cpu?.Cores;
                if (actual is null) issues.Add($"Requires CPU cores >= {constraints.Cpu.MinCores}, but RuntimeEnvironment.SystemInfo.Cpu.Cores is missing.");
                else if (actual < constraints.Cpu.MinCores) issues.Add($"Requires CPU cores >= {constraints.Cpu.MinCores}, actual: {actual}.");
            }

            if (constraints.Cpu.MinLogicalProcessors is not null)
            {
                var actual = sys.Cpu?.LogicalProcessors;
                if (actual is null) issues.Add($"Requires logical processors >= {constraints.Cpu.MinLogicalProcessors}, but RuntimeEnvironment.SystemInfo.Cpu.LogicalProcessors is missing.");
                else if (actual < constraints.Cpu.MinLogicalProcessors) issues.Add($"Requires logical processors >= {constraints.Cpu.MinLogicalProcessors}, actual: {actual}.");
            }

            if (constraints.Cpu.MinClockGHz is not null)
            {
                var actual = sys.Cpu?.ClockGHz;
                if (actual is null) issues.Add($"Requires CPU clock >= {constraints.Cpu.MinClockGHz} GHz, but RuntimeEnvironment.SystemInfo.Cpu.ClockGHz is missing.");
                else if (actual < constraints.Cpu.MinClockGHz) issues.Add($"Requires CPU clock >= {constraints.Cpu.MinClockGHz} GHz, actual: {actual} GHz.");
            }
        }

        // Memory
        if (constraints.Memory?.MinGb is not null)
        {
            var actual = sys.Memory?.CapacityInGb;
            if (actual is null) issues.Add($"Requires memory >= {constraints.Memory.MinGb} GB, but RuntimeEnvironment.SystemInfo.Memory.CapacityInGb is missing.");
            else if (actual < constraints.Memory.MinGb) issues.Add($"Requires memory >= {constraints.Memory.MinGb} GB, actual: {actual} GB.");
        }

        // GPU
        if (constraints.Gpu is not null)
        {
            if (constraints.Gpu.MinMemoryGb is not null)
            {
                var actual = sys.Gpu?.MemoryInGb;
                if (actual is null) issues.Add($"Requires GPU memory >= {constraints.Gpu.MinMemoryGb} GB, but RuntimeEnvironment.SystemInfo.Gpu.MemoryInGb is missing.");
                else if (actual < constraints.Gpu.MinMemoryGb) issues.Add($"Requires GPU memory >= {constraints.Gpu.MinMemoryGb} GB, actual: {actual} GB.");
            }

            if (constraints.Gpu.Vendor is not null)
            {
                var actualVendor = sys.Gpu?.Vendor;
                if (string.IsNullOrWhiteSpace(actualVendor))
                {
                    issues.Add($"Requires GPU vendor '{constraints.Gpu.Vendor}', but RuntimeEnvironment.SystemInfo.Gpu.Vendor is missing.");
                }
                else if (!Enum.TryParse<GpuVendor>(actualVendor.Trim(), ignoreCase: true, out var parsed) || parsed != constraints.Gpu.Vendor)
                {
                    issues.Add($"Requires GPU vendor '{constraints.Gpu.Vendor}', actual: '{actualVendor}'.");
                }
            }
        }

        // Storage
        if (constraints.Storage is not null)
        {
            if (constraints.Storage.MinFreeSpaceGb is not null)
            {
                var actual = sys.Storage?.FreeSpaceInGb;
                if (actual is null) issues.Add($"Requires free storage >= {constraints.Storage.MinFreeSpaceGb} GB, but RuntimeEnvironment.SystemInfo.Storage.FreeSpaceInGb is missing.");
                else if (actual < constraints.Storage.MinFreeSpaceGb) issues.Add($"Requires free storage >= {constraints.Storage.MinFreeSpaceGb} GB, actual: {actual} GB.");
            }

            if (constraints.Storage.MinReadSpeedMbps is not null)
            {
                var actual = sys.Storage?.ReadSpeedInMbps;
                if (actual is null) issues.Add($"Requires storage read speed >= {constraints.Storage.MinReadSpeedMbps} Mbps, but RuntimeEnvironment.SystemInfo.Storage.ReadSpeedInMbps is missing.");
                else if (actual < constraints.Storage.MinReadSpeedMbps) issues.Add($"Requires storage read speed >= {constraints.Storage.MinReadSpeedMbps} Mbps, actual: {actual} Mbps.");
            }

            if (constraints.Storage.Type is not null)
            {
                var actualType = sys.Storage?.Type;
                if (string.IsNullOrWhiteSpace(actualType))
                {
                    issues.Add($"Requires storage type '{constraints.Storage.Type}', but RuntimeEnvironment.SystemInfo.Storage.Type is missing.");
                }
                else if (!Enum.TryParse<StorageType>(actualType.Trim(), ignoreCase: true, out var parsed) || parsed != constraints.Storage.Type)
                {
                    issues.Add($"Requires storage type '{constraints.Storage.Type}', actual: '{actualType}'.");
                }
            }
        }

        // Parallelism
        if (constraints.Parallelism is not null)
        {
            if (constraints.Parallelism.MinThreads is not null)
            {
                var actual = sys.Parallelism?.Threads;
                if (actual is null) issues.Add($"Requires threads >= {constraints.Parallelism.MinThreads}, but RuntimeEnvironment.SystemInfo.Parallelism.Threads is missing.");
                else if (actual < constraints.Parallelism.MinThreads) issues.Add($"Requires threads >= {constraints.Parallelism.MinThreads}, actual: {actual}.");
            }

            if (constraints.Parallelism.MinConcurrentProcesses is not null)
            {
                var actual = sys.Parallelism?.ConcurrentProcesses;
                if (actual is null) issues.Add($"Requires concurrent processes >= {constraints.Parallelism.MinConcurrentProcesses}, but RuntimeEnvironment.SystemInfo.Parallelism.ConcurrentProcesses is missing.");
                else if (actual < constraints.Parallelism.MinConcurrentProcesses) issues.Add($"Requires concurrent processes >= {constraints.Parallelism.MinConcurrentProcesses}, actual: {actual}.");
            }

            if (constraints.Parallelism.RequiresGpuParallelism is true)
            {
                var actual = sys.Parallelism?.RequiresGpuParallelism;
                if (actual is not true)
                    issues.Add("Requires GPU parallelism, but RuntimeEnvironment.SystemInfo.Parallelism.RequiresGpuParallelism is not true.");
            }
        }
    }
}