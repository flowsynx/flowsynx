using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Services;

public class FlowSynxLocation : ILocation
{
    private readonly ILogger<FlowSynxLocation> _logger;
    private readonly string? _rootLocation = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

    public FlowSynxLocation(ILogger<FlowSynxLocation> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
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
        try
        {
            if (_rootLocation is not null) 
                return _rootLocation;

            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationLocation, Resources.FlowSynxLocationRootLocationNotFound);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationLocation, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
    #endregion
}
