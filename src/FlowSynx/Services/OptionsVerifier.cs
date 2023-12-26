using EnsureThat;
using FlowSynx.Commands;
using FlowSynx.Core.Exceptions;
using FlowSynx.IO.Serialization;

namespace FlowSynx.Services;

public class OptionsVerifier: IOptionsVerifier
{
    private readonly ILogger<OptionsVerifier> _logger;
    private readonly ISerializer _serializer;

    public OptionsVerifier(ILogger<OptionsVerifier> logger, ISerializer serializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(serializer, nameof(serializer));
        _logger = logger;
        _serializer = serializer;
    }

    public void Verify(ref RootCommandOptions options)
    {
        options.ConfigFile = CheckConfigurationPath(options.ConfigFile);
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
            throw new Exception(string.Format(Resources.OptionsVerifierTheEnteredConfigFileIsNotExist, configPath));
        }

        return newPath;
    }

    private string GetExecutingPath()
    {
        var fullPath = Path.GetDirectoryName(System.AppContext.BaseDirectory);
        if (string.IsNullOrEmpty(fullPath))
            throw new ApiBaseException(Resources.OptionsVerifierErrorInReadingExecutableApplicationPath);

        return fullPath;
    }
}
