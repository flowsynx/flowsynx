using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginExtractor : IPluginExtractor
{
    private readonly ILogger<PluginExtractor> _logger;

    public PluginExtractor(ILogger<PluginExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractPluginAsync(string pluginPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(pluginPath))
            throw new ArgumentException("Package path cannot be null or empty", nameof(pluginPath));

        if (!File.Exists(pluginPath))
            throw new FileNotFoundException("The specified plugin file does not exist", pluginPath);

        if (Path.GetExtension(pluginPath).ToLower() != ".zip")
            throw new InvalidOperationException("The provided plugin is not a valid ZIP file");

        var parentDirectory = Path.GetDirectoryName(pluginPath);
        if (string.IsNullOrEmpty(parentDirectory))
            throw new InvalidOperationException("The provided plugin location is not a valid");
        
        var targetDirectory = Path.Combine(parentDirectory, Path.GetFileNameWithoutExtension(pluginPath));

        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        try
        {
            await Task.Run(() => ZipFile.ExtractToDirectory(pluginPath, targetDirectory), cancellationToken);
            _logger.LogInformation($"Plugin successfully extracted to: {targetDirectory}");
            return targetDirectory;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while extracting the package: {ex.Message}", ex);
        }
    }
}