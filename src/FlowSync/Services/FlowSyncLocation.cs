using System.Reflection;
using FlowSync.Core.Serialization;
using FlowSync.Core.Services;

namespace FlowSync.Services;

public class FlowSyncLocation : ILocation
{
    private readonly ILogger<FlowSyncLocation> _logger;
    private readonly ISerializer _serializer;
    private readonly string? _rootLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private readonly string _configurationFile;

    public FlowSyncLocation(ILogger<FlowSyncLocation> logger, ISerializer serializer)
    {
        _logger = logger;
        _serializer = serializer;

        if (_rootLocation == null)
        {
            logger.LogError("Base location not found");
            throw new Exception("Base location not found");
        }

        _configurationFile = Path.Combine(_rootLocation, "configuration.json");
        CheckConfigurationFile(_configurationFile);
    }

    public string RootLocation => GetRootLocation();

    public string ConfigurationFile => GetConfigurationFile();

    #region MyRegion
    private string GetRootLocation()
    {
        if (_rootLocation is null)
            throw new Exception("Root location not found");
        return _rootLocation;
    }
    
    private string GetConfigurationFile()
    {
        CheckConfigurationFile(_configurationFile);
        return _configurationFile;
    }

    private void CheckLocation(string location)
    {
        try
        {
            if (!Directory.Exists(location))
                Directory.CreateDirectory(location);
        }
        catch (Exception e)
        {
            _logger.LogError($"Error in Checking location: {e.Message}");
            throw;
        }
    }

    private void CheckConfigurationFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                File.WriteAllText(path, _serializer.Serialize(new { }));
        }
        catch (Exception e)
        {
            _logger.LogError($"Error in Checking configuration file: {e.Message}");
            throw;
        }
    }
    #endregion
}
