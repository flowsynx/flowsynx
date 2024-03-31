using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FlowSynx.Plugin.Storage.Memory;

public class MemoryMetricsClient
{
    public MemoryMetrics GetMetrics()
    {
        return IsUnix() ? GetUnixMetrics() : GetWindowsMetrics();
    }

    private bool IsUnix()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    private MemoryMetrics GetWindowsMetrics()
    {
        var output = "";

        var info = new ProcessStartInfo
        {
            FileName = "wmic",
            Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value",
            RedirectStandardOutput = true
        };

        using (var process = Process.Start(info))
        {
            output = process.StandardOutput.ReadToEnd();
        }

        var lines = output.Trim().Split("\n");
        var freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
        var totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

        var metrics = new MemoryMetrics
        {
            Total = long.Parse(totalMemoryParts[1]) * 1024,
            Free = long.Parse(freeMemoryParts[1]) * 1024
        };
        metrics.Used = metrics.Total - metrics.Free;

        return metrics;
    }

    private MemoryMetrics GetUnixMetrics()
    {
        var output = "";

        var info = new ProcessStartInfo("free -m")
        {
            FileName = "/bin/bash",
            Arguments = "-c \"free -m\"",
            RedirectStandardOutput = true
        };

        using (var process = Process.Start(info))
        {
            output = process.StandardOutput.ReadToEnd();
        }

        var lines = output.Split("\n");
        var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

        return new MemoryMetrics
        {
            Total = long.Parse(memory[1]) * 1024,
            Used = long.Parse(memory[2]) * 1024,
            Free = long.Parse(memory[3]) * 1024
        };
    }
}