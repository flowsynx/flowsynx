using FlowSync.Core.Common.Services;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FlowSync.Services;

public class EnvironmentVariablesManager : IEnvironmentVariablesManager
{
    public string? Get(string variableName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return System.Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);

        string? result = null;
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c echo {variableName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        proc.Start();
        while (!proc.StandardOutput.EndOfStream)
            result = proc.StandardOutput.ReadLine();

        proc.WaitForExit();
        return result;

    }

    public void Set(string variableName, string value)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            System.Environment.SetEnvironmentVariable(variableName, value, EnvironmentVariableTarget.Machine);
            return;
        }

        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c export {variableName}={value}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        proc.Start();
        proc.WaitForExit();
    }
}