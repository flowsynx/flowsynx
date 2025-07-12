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

    public Version Version => GetApplicationVersion();

    #region Private function
    private Version GetApplicationVersion()
    {
        try
        {
            var attributes = Assembly
                .GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);

            var versionString = attributes.Length == 0
                ? "0.0.0.0" // Default if no version is found
                : ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;

            // Parse the string to a Version object
            if (Version.TryParse(versionString, out var version))
            {
                return version;
            }

            // Fallback if parsing fails
            _logger.LogWarning("Failed to parse application version. Using default 0.0.0.0.");
            return new Version(0, 0, 0, 0);
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