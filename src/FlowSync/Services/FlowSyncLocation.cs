using EnsureThat;
using FlowSync.Core.Common.Services;

namespace FlowSync.Services;

public class FlowSyncLocation : ILocation
{
    private readonly ILogger<FlowSyncLocation> _logger;
    private readonly string? _rootLocation = Path.GetDirectoryName(System.AppContext.BaseDirectory);

    public FlowSyncLocation(ILogger<FlowSyncLocation> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;

        if (_rootLocation == null)
        {
            logger.LogError("Base location not found");
            throw new Exception("Base location not found");
        }
    }

    public string RootLocation => GetRootLocation();

    #region MyRegion
    private string GetRootLocation()
    {
        if (_rootLocation is not null) return _rootLocation;
        _logger.LogError("Root location not found");
        throw new Exception("Root location not found");
    }
    #endregion
}
