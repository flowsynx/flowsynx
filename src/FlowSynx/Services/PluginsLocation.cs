using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.PluginCore.Exceptions;
namespace FlowSynx.Services;

public class PluginsLocation : IPluginsLocation
{
    private readonly ILogger<PluginsLocation> _logger;
    private readonly ILocalization _localization;
    private readonly string? _rootLocation = System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

    public PluginsLocation(
        ILogger<PluginsLocation> logger,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _localization = localization;
        if (_rootLocation != null) 
            return;

        logger.LogError("Base location not found");
        throw new Exception(_localization.Get("FlowSynxLocationBaseLocationNotFound"));
    }

    public string Path => GetPluginsPath();

    #region MyRegion
    private string GetPluginsPath()
    {
        try
        {
            if (_rootLocation is not null)
            {
                var pluginsPath = System.IO.Path.Combine(_rootLocation, "plugins");
                if (!Directory.Exists(pluginsPath))
                    Directory.CreateDirectory(pluginsPath);
                
                return pluginsPath;
            }

            var errorMessage = new ErrorMessage((int)ErrorCode.PluginsLocation, _localization.Get("FlowSynxLocationRootLocationNotFound"));
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginsLocation, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
    #endregion
}
