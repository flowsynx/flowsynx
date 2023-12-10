using EnsureThat;
using FlowSync.Core.Configuration;
using FlowSync.Core.Serialization;
using FlowSync.Infrastructure.Exceptions;
using FlowSync.Infrastructure.IO;
using Microsoft.Extensions.Logging;

namespace FlowSync.Infrastructure.Services;

public class ConfigurationManager : IConfigurationManager
{
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly ConfigurationPath _options;
    private readonly IFileReader _fileReader;
    private readonly IFileWriter _fileWriter;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public ConfigurationManager(ILogger<ConfigurationManager> logger, ConfigurationPath options, 
        IFileReader fileReader, IFileWriter fileWriter, ISerializer serializer, 
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(options, nameof(options));
        EnsureArg.IsNotNull(fileReader, nameof(fileReader));
        EnsureArg.IsNotNull(fileWriter, nameof(fileWriter));
        EnsureArg.IsNotNull(serializer, nameof(serializer));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _options = options;
        _fileReader = fileReader;
        _fileWriter = fileWriter;
        _serializer = serializer;
        _deserializer = deserializer;
    }

    public ConfigurationStatus AddSetting(ConfigurationItem configuration)
    {
        var contents = _fileReader.Read(_options.Path);
        var data = _deserializer.Deserialize<Configuration>(contents);

        var convertedData = data?.Configurations;
        var findItems = convertedData?.Where(x => 
            string.Equals(x.Name, configuration.Name, StringComparison.CurrentCultureIgnoreCase));

        if (findItems != null && findItems.Any())
        {
            _logger.LogWarning($"{configuration.Name} is already exist.");
            return ConfigurationStatus.Exist;
        }

        convertedData?.Add(configuration);

        var newSetting = new Configuration()
        {
            Configurations = convertedData!
        };

        var dataToWrite = _serializer.Serialize(newSetting);
        _fileWriter.Write(_options.Path, dataToWrite);
        return ConfigurationStatus.Added;
    }

    public bool DeleteSetting(string name)
    {
        var contents = _fileReader.Read(_options.Path);
        var data = _deserializer.Deserialize<Configuration>(contents);

        if (data is null)
        {
            _logger.LogWarning($"No setting found!");
            return false;
        }

        var convertedData = data.Configurations.ToList();
        var item = convertedData.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));

        if (item == null)
        {
            _logger.LogWarning($"{0} is not found.", name);
            return false;
        }

        convertedData.Remove(item);

        var newSetting = new Configuration()
        {
            Configurations = convertedData!
        };

        var dataToWrite = _serializer.Serialize(newSetting);
        return _fileWriter.Write(_options.Path, dataToWrite);
    }

    public ConfigurationItem GetSetting(string name)
    {
        var contents = _fileReader.Read(_options.Path);
        var data = _deserializer.Deserialize<Configuration>(contents);

        var result = data?.Configurations.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));

        if (result != null) return result;
        _logger.LogWarning($"{name} is not found.");
        throw new ConfigurationException(string.Format(FlowSyncInfrastructureResource.ConfigurationManagerItemNotFoumd, name));
    }

    public IEnumerable<ConfigurationItem> GetSettings()
    {
        var result = new List<ConfigurationItem>();
        var contents = _fileReader.Read(_options.Path);
        var deserializeResult = _deserializer.Deserialize<Configuration>(contents);
        return deserializeResult == null ? result : deserializeResult.Configurations;
    }

    public bool IsExist(string name)
    {
        var contents = _fileReader.Read(_options.Path);
        var data = _deserializer.Deserialize<Configuration>(contents);

        var result = data?.Configurations.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));

        if (result != null) return true;
        _logger.LogWarning($"{name} is not found.");
        return false;
    }
}