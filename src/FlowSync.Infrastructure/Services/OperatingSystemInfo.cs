using FlowSync.Core.Common.Services;
using System.Runtime.InteropServices;

namespace FlowSync.Infrastructure.Services;

public class OperatingSystemInfo : IOperatingSystemInfo
{
    public string? Version => $"{RuntimeInformation.OSDescription} ({GetArchFriendlyBits(RuntimeInformation.OSArchitecture)} bits)";
    public string? Type => OperatingSystemType();
    public string? Architecture => RuntimeInformation.ProcessArchitecture.ToString();

    private string OperatingSystemType()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "OSX";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return "OSX";

        return "Windows";
    }

    private int GetArchFriendlyBits(Architecture architecture)
    {
        return architecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => 64,
            System.Runtime.InteropServices.Architecture.X86 => 32,
            System.Runtime.InteropServices.Architecture.Arm64 => 64,
            System.Runtime.InteropServices.Architecture.Arm => 32,
            System.Runtime.InteropServices.Architecture.Wasm => -1,
            System.Runtime.InteropServices.Architecture.S390x => -1,
            _ => -1,
        };
    }
}