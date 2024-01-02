using System.Diagnostics;
using EnsureThat;
using FlowSynx.Environment;

namespace FlowSynx.Services;

public class FlowSynxVersion : IVersion
{
    private readonly ILogger<FlowSynxLocation> _logger;

    public FlowSynxVersion(ILogger<FlowSynxLocation> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
    }

    public string Version => GetApplicationVersion();

    #region Private function
    private string GetApplicationVersion()
    {
        var assemblyLocation = System.Environment.ProcessPath;

        if (string.IsNullOrEmpty(assemblyLocation))
            return "1.0.0.0";

        var versionInfo = FileVersionInfo.GetVersionInfo(assemblyLocation);
        return versionInfo.ProductVersion ?? "1.0.0.0";
    }
    #endregion
}
