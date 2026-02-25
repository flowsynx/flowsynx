using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public sealed class RuntimeEnvironmentProvider : IRuntimeEnvironmentProvider
{
    private const int GbDivisor = 1024 * 1024 * 1024;
    private readonly IVersion _version; 

    public RuntimeEnvironmentProvider(IVersion version)
    {
        _version = version;
    }

    public Application.Models.RuntimeEnvironment GetCurrent()
    {
        return new Application.Models.RuntimeEnvironment
        {
            RuntimeVersion = _version.Version.ToString(),
            Platform = $"{GetOsToken()}/{GetArchToken()}",
            SystemInfo = BuildSystemInfo()
        };
    }

    private static SystemInfo BuildSystemInfo()
    {
        var processorCount = Environment.ProcessorCount;

        return new SystemInfo
        {
            Cpu = new CpuInfo
            {
                Cores = processorCount,
                LogicalProcessors = processorCount,
                ClockGHz = GetCpuClockGHz()
            },
            Memory = new MemoryInfo
            {
                CapacityInGb = TryGetApproxAvailableMemoryGb()
            },
            Storage = new StorageInfo
            {
                FreeSpaceInGb = TryGetFreeDiskSpaceGb()
            },
            Parallelism = new ParallelismInfo
            {
                Threads = processorCount,
                ConcurrentProcesses = processorCount,
                RequiresGpuParallelism = false
            },
            Gpu = TryGetGpuInfo()
        };
    }

    #region Platform

    private static string GetOsToken()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macos";
        return "unknown";
    }

    private static string GetArchToken() =>
        RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "386",
            Architecture.Arm => "arm",
            _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()
        };

    #endregion

    #region Memory / Storage

    private static int? TryGetApproxAvailableMemoryGb()
    {
        try
        {
            var bytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            if (bytes <= 0) return null;

            return (int)Math.Round(bytes / (double)GbDivisor);
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
            if (string.IsNullOrWhiteSpace(root)) return null;

            var drive = new DriveInfo(root);
            if (!drive.IsReady) return null;

            return (int)Math.Floor(drive.AvailableFreeSpace / (double)GbDivisor);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region GPU

    private static GpuInfo? TryGetGpuInfo()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsGpu();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetLinuxGpu();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetMacGpu();
        }
        catch
        {
            // ignored intentionally
        }

        return null;
    }

    private static GpuInfo? GetWindowsGpu()
    {
        using var searcher =
            new System.Management.ManagementObjectSearcher(
                "SELECT AdapterRAM, Name FROM Win32_VideoController");

        foreach (var obj in searcher.Get())
        {
            if (obj["AdapterRAM"] is null || obj["Name"] is null)
                continue;

            var ramBytes = Convert.ToUInt64(obj["AdapterRAM"]);
            return new GpuInfo
            {
                MemoryInGb = (int)(ramBytes / GbDivisor),
                Vendor = ParseGpuVendor(obj["Name"]?.ToString())
            };
        }

        return null;
    }

    private static GpuInfo? GetLinuxGpu()
    {
        var output = RunProcess("lspci", "-vnn");
        if (string.IsNullOrWhiteSpace(output)) return null;

        var vgaLine = output
            .Split('\n')
            .FirstOrDefault(l => l.Contains("vga", StringComparison.OrdinalIgnoreCase));

        if (vgaLine is null) return null;

        return new GpuInfo
        {
            Vendor = ParseGpuVendor(vgaLine),
            MemoryInGb = null
        };
    }

    private static GpuInfo? GetMacGpu()
    {
        var output = RunProcess("system_profiler", "SPDisplaysDataType");
        if (string.IsNullOrWhiteSpace(output)) return null;

        var lines = output.Split('\n');

        var memoryGb = lines
            .Select(l => Regex.Match(l, @"(\d+)\s*GB"))
            .Where(m => m.Success)
            .Select(m => int.Parse(m.Groups[1].Value))
            .Cast<int?>()
            .FirstOrDefault();

        var vendorLine = lines.FirstOrDefault(l =>
            l.Contains("Vendor", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("Chipset Model", StringComparison.OrdinalIgnoreCase));

        var vendor = vendorLine?.Split(':').ElementAtOrDefault(1)?.Trim();

        return new GpuInfo
        {
            MemoryInGb = memoryGb,
            Vendor = vendor
        };
    }

    private static string? ParseGpuVendor(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var value = input.ToLowerInvariant();

        if (value.Contains("nvidia")) return "NVIDIA";
        if (value.Contains("amd") || value.Contains("radeon")) return "AMD";
        if (value.Contains("intel")) return "Intel";
        if (value.Contains("apple") || value.Contains("m1") || value.Contains("m2")) return "Apple";

        return null;
    }

    #endregion

    #region CPU Clock

    public static double? GetCpuClockGHz()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetWindowsClock();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return GetLinuxClock();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return GetMacClock();

        return null;
    }

    private static double? GetWindowsClock()
    {
        try
        {
            using var searcher =
                new System.Management.ManagementObjectSearcher(
                    "SELECT MaxClockSpeed FROM Win32_Processor");

            foreach (var item in searcher.Get())
            {
                var mhz = Convert.ToDouble(item["MaxClockSpeed"]);
                return mhz / 1000.0;
            }
        }
        catch { }

        return null;
    }

    private static double? GetLinuxClock()
    {
        try
        {
            foreach (var line in File.ReadLines("/proc/cpuinfo"))
            {
                if (!line.StartsWith("cpu MHz", StringComparison.OrdinalIgnoreCase))
                    continue;

                var mhz = double.Parse(
                    line.Split(':')[1].Trim(),
                    System.Globalization.CultureInfo.InvariantCulture);

                return mhz / 1000.0;
            }
        }
        catch { }

        return null;
    }

    private static double? GetMacClock()
    {
        try
        {
            var output = RunProcess("sysctl", "-n hw.cpufrequency");

            if (long.TryParse(output.Trim(), out var hz))
                return hz / 1_000_000_000.0;
        }
        catch { }

        return null;
    }

    #endregion

    #region Process

    private static string RunProcess(string fileName, string arguments, int timeoutSeconds = 2)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        if (!process.WaitForExit(timeoutSeconds * 1000))
        {
            try { process.Kill(); } catch { }
            return string.Empty;
        }

        return process.StandardOutput.ReadToEnd();
    }

    #endregion
}