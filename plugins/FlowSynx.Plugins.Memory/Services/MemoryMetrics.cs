using System.Diagnostics;
using System.Runtime.InteropServices;
using FlowSynx.Connectors.Storage.Memory.Models;

namespace FlowSynx.Connectors.Storage.Memory.Services;

public class MemoryMetrics: IMemoryMetrics
{
    public Models.MemoryMetrics GetMetrics()
    {
        return IsUnix() ? GetUnixMetrics() : GetWindowsMetrics();
    }

    #region internal methods
    private bool IsUnix()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    private Models.MemoryMetrics GetWindowsMetrics()
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
            if (process != null)
                output = process.StandardOutput.ReadToEnd();
        }

        var lines = output.Trim().Split("\n");
        var freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
        var totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

        var metrics = new Models.MemoryMetrics
        {
            Total = long.Parse(totalMemoryParts[1]) * 1024,
            Free = long.Parse(freeMemoryParts[1]) * 1024
        };
        metrics.Used = metrics.Total - metrics.Free;

        return metrics;
    }

    private Models.MemoryMetrics GetUnixMetrics()
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
            if (process != null)
                output = process.StandardOutput.ReadToEnd();
        }

        var lines = output.Split("\n");
        var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

        return new Models.MemoryMetrics
        {
            Total = long.Parse(memory[1]) * 1024,
            Used = long.Parse(memory[2]) * 1024,
            Free = long.Parse(memory[3]) * 1024
        };
    }
    #endregion
}