using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using System.Reflection;

namespace FlowSynx.Services;

public class FlowSynxVersion : IVersion
{
    private readonly ILogger<FlowSynxVersion> _logger;

    public FlowSynxVersion(ILogger<FlowSynxVersion> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public string Version => GetApplicationVersion();

    #region Private function
    private string GetApplicationVersion()
    {
        try
        {
            var attributes = Assembly
                .GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);

            return attributes.Length == 0 ? "" : ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationVersion, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
    #endregion
}