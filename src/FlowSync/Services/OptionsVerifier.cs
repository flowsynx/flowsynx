using FlowSync.Commands;
using FlowSync.Core.Exceptions;
using FlowSync.Core.Serialization;

namespace FlowSync.Services;

public class OptionsVerifier: IOptionsVerifier
{
    private readonly ILogger<OptionsVerifier> _logger;
    private readonly ISerializer _serializer;

    public OptionsVerifier(ILogger<OptionsVerifier> logger, ISerializer serializer)
    {
        _logger = logger;
        _serializer = serializer;
    }

    public void Verify(ref CommandOptions options)
    {
        options.Config = CheckConfigurationPath(options.Config);
    }

    public string CheckConfigurationPath(string configPath)
    {
        if (string.IsNullOrEmpty(configPath))
        {
            var defaultConfigPath = Path.Combine(GetExecutingPath(), "configuration.json");
            if (!File.Exists(defaultConfigPath))
                File.WriteAllText(defaultConfigPath, _serializer.Serialize(new {}));
            return defaultConfigPath;
        }

        if (File.Exists(configPath)) return configPath;

        var newPath = Path.Combine(GetExecutingPath(), configPath);
        if (!File.Exists(newPath))
        {
            throw new Exception($"The entered config file '{configPath}' is not exist!");
        }

        return newPath;
    }

    private string GetExecutingPath()
    {
        var fullPath = Path.GetDirectoryName(System.AppContext.BaseDirectory);
        if (string.IsNullOrEmpty(fullPath))
            throw new ApiException($"Error in reading executable application path.");

        return fullPath;
    }
}
