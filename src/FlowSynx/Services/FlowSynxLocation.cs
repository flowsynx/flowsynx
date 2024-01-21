using EnsureThat;
using FlowSynx.Core.Services;

namespace FlowSynx.Services;

public class FlowSynxLocation : ILocation
{
    private readonly ILogger<FlowSynxLocation> _logger;
    private readonly string? _rootLocation = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

    public FlowSynxLocation(ILogger<FlowSynxLocation> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;

        if (_rootLocation == null)
        {
            logger.LogError("Base location not found");
            throw new Exception(Resources.FlowSynxLocationBaseLocationNotFound);
        }
    }

    public string RootLocation => GetRootLocation();

    #region MyRegion
    private string GetRootLocation()
    {
        if (_rootLocation is not null) return _rootLocation;
        _logger.LogError("Root location not found");
        throw new Exception(Resources.FlowSynxLocationRootLocationNotFound);
    }
    #endregion
}
