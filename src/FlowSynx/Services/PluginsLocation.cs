using FlowSynx.Application.Models;
using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.PluginCore.Exceptions;
namespace FlowSynx.Services;

public class PluginsLocation : IPluginsLocation
{
    private readonly ILogger<PluginsLocation> _logger;
    private readonly string? _rootLocation = System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

    public PluginsLocation(ILogger<PluginsLocation> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;

        if (_rootLocation == null)
        {
            logger.LogError("Base location not found");
            throw new Exception(Resources.FlowSynxLocationBaseLocationNotFound);
        }
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

            var errorMessage = new ErrorMessage((int)ErrorCode.PluginsLocation, Resources.FlowSynxLocationRootLocationNotFound);
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
