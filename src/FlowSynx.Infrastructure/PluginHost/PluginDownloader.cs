using FlowSynx.Application.Serialization;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Security.Cryptography;

namespace FlowSynx.Infrastructure.PluginHost;

public class PluginDownloader : IPluginDownloader
{
    private readonly ILogger<PluginDownloader> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJsonDeserializer _jsonDeserializer;

    public PluginDownloader(ILogger<PluginDownloader> logger, IHttpClientFactory httpClientFactory,
        IJsonDeserializer jsonDeserializer)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _jsonDeserializer = jsonDeserializer;
    }

    public async Task<byte[]> GetPluginDataAsync(string url)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }

        throw new Exception("Failed to fetch file from URL.");
    }

    public async Task<PluginInstallMetadata> GetPluginMetadataAsync(string url)
    {
        var client = _httpClientFactory.CreateClient("PluginRegistry");
        var response = await client.GetStringAsync(url);
        var metadata = _jsonDeserializer.Deserialize<PluginInstallMetadata>(response);

        return metadata;
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

    public bool ValidateChecksum(string pluginPath, string expectedChecksum)
    {
        var pluginData = File.ReadAllBytes(pluginPath);
        string computedChecksum = ComputeChecksum(pluginData);
        return computedChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    private string ComputeChecksum(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}