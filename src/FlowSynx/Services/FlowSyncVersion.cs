using System.Diagnostics;
using System.Reflection;
using EnsureThat;
using FlowSynx.Core.Exceptions;
using FlowSynx.Environment;

namespace FlowSynx.Services;

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
        Assembly? thisAssembly = null;
        try
        {
            thisAssembly = Assembly.GetEntryAssembly();
        }
        finally
        {
            if (thisAssembly is null)
            {
                _logger.LogWarning(Resources.FlowSyncVersionEntryAssemblyNotFound);
                thisAssembly = Assembly.GetExecutingAssembly();
            }
        }

        if (thisAssembly == null)
            throw new ApiBaseException(Resources.FlowSyncVersionErrorInReadingExecutableApplication);

        var fullAssemblyName = thisAssembly.Location;
        var versionInfo = FileVersionInfo.GetVersionInfo(fullAssemblyName);
        return versionInfo.ProductVersion ?? "1.0.0.0";
    }
    #endregion
}
