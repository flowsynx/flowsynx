using Microsoft.Extensions.Logging;
using FlowSync.Core.Services;
using FlowSync.Storage.AzureBlob;
using FlowSync.Storage.Local;
using FlowSync.Core.Utilities.Plugins;

namespace FlowSync.Persistence.Json.Services;

public class PluginsManager : IPluginsManager
{
    private readonly ILogger<PluginsManager> _logger;

    public PluginsManager(ILogger<PluginsManager> logger)
    {
        _logger = logger;
    }

    public PluginItem GetPlugin(string name)
    {
        var result = Plugins().FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        if (result != null) 
            return result;

        _logger.LogError($"Plugin {name} could not found!");
        throw new ArgumentException("Plugin could not found!");
    }
    
    public bool IsExist(string name)
    {
        var result = Plugins().FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        return result != null;
    }

    public IEnumerable<PluginItem> Plugins()
    {
        return new List<PluginItem>()
        {
            new PluginItem() { Name = "AzureBlob", Type = typeof(AzureBlob) },
            new PluginItem() { Name = "LocalFileSystem", Type = typeof(LocalFileSystem) }
        };
    }
}