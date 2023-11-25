using System.Diagnostics;
using System.Reflection;
using EnsureThat;
using FlowSync.Core.Common.Services;
using FlowSync.Core.Exceptions;

namespace FlowSync.Services;

public class FlowSyncVersion : IVersion
{
    private readonly ILogger<FlowSyncLocation> _logger;
    private readonly string? _rootLocation = Path.GetDirectoryName(System.AppContext.BaseDirectory);

    public FlowSyncVersion(ILogger<FlowSyncLocation> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
    }

    public string Version => GetApplicationVersion();

    #region MyRegion
    private string GetApplicationVersion()
    {

        var assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        if (assembly == null)
            throw new ApiBaseException($"Error in reading executable application.");

        Assembly? thisAssembly = null;
        try
        {
            thisAssembly = Assembly.GetEntryAssembly();
        }
        finally
        {
            if (thisAssembly is null)
            {
                _logger.LogWarning("The EntryAssembly not found. Getting ExecutingAssembly!");
                thisAssembly = Assembly.GetExecutingAssembly();
            }
        }

        var fullAssemblyName = thisAssembly.Location;
        var versionInfo = FileVersionInfo.GetVersionInfo(fullAssemblyName);
        return versionInfo.ProductVersion ?? "V1.0";
    }
    #endregion
}
