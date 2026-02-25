using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Models;
using System.Runtime.InteropServices;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public sealed class RuntimeEnvironmentProvider : IRuntimeEnvironmentProvider
{
    public Application.Models.RuntimeEnvironment GetCurrent()
    {
        return new Application.Models.RuntimeEnvironment
        {
            RuntimeVersion = Environment.Version.ToString(),
            Platform = $"{GetOsToken()}/{GetArchToken()}",
            SystemInfo = new SystemInfo
            {
                Cpu = new CpuInfo
                {
                    Cores = Environment.ProcessorCount,
                    LogicalProcessors = Environment.ProcessorCount
                    // ClockGHz: unknown cross-platform => leave null
                },
                Memory = new MemoryInfo
                {
                    CapacityInGb = TryGetApproxAvailableMemoryGb()
                },
                Storage = new StorageInfo
                {
                    FreeSpaceInGb = TryGetFreeDiskSpaceGb()
                    // Type/ReadSpeedInMbps: unknown cross-platform => leave null
                },
                Parallelism = new ParallelismInfo
                {
                    Threads = Environment.ProcessorCount,
                    ConcurrentProcesses = Environment.ProcessorCount,
                    RequiresGpuParallelism = false
                }
                // GPU: unknown cross-platform => leave null
            }
        };
    }

    private static string GetOsToken()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macos";
        return "unknown";
    }

    private static string GetArchToken()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "386",
            Architecture.Arm => "arm",
            _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()
        };
    }

    private static int? TryGetApproxAvailableMemoryGb()
    {
        try
        {
            var bytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            if (bytes <= 0)
                return null;

            var gb = bytes / 1024d / 1024d / 1024d;
            return (int)Math.Round(gb);
        }
        catch
        {
            return null;
        }
    }

    private static int? TryGetFreeDiskSpaceGb()
    {
        try
        {
            var root = Path.GetPathRoot(AppContext.BaseDirectory);
            if (string.IsNullOrWhiteSpace(root))
                return null;

            var drive = new DriveInfo(root);
            if (!drive.IsReady)
                return null;

            var gb = drive.AvailableFreeSpace / 1024d / 1024d / 1024d;
            return (int)Math.Floor(gb);
        }
        catch
        {
            return null;
        }
    }
}