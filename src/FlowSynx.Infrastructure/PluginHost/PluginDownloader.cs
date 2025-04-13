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

    public async Task<PluginInstallMetadata> GetPluginMetadataAsync(string url, string pluginType, string pluginVersion)
    {
        var client = _httpClientFactory.CreateClient("PluginRegistry");
        var mainUrl = new Uri(url);
        var indexUrl = new Uri(mainUrl, "index.json");
        var response = await client.GetStringAsync(indexUrl);
        var plugins = _jsonDeserializer.Deserialize<List<PluginInstallMetadata>>(response);
        var metadata = plugins.FirstOrDefault(x=>x.Type.ToLower() == pluginType.ToLower() && x.Version == pluginVersion);

        if (metadata == null)
            throw new Exception($"No plugin with type '{pluginType}' and version '{pluginVersion}' found.");

        return metadata;
    }

    public async Task ExtractPluginAsync(string pluginDirectory, byte[] data, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => DeleteAllFiles(pluginDirectory), cancellationToken);
            await ExtractZipFromBytesAsync(pluginDirectory, data, cancellationToken);
            _logger.LogInformation($"Plugin successfully extracted to: {pluginDirectory}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while extracting the package: {ex.Message}", ex);
        }
    }

    public bool ValidateChecksum(byte[] data, string expectedChecksum)
    {
        string computedChecksum = ComputeChecksum(data);
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

    private void DeleteAllFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning($"Directory not found: {directoryPath}");
            return;
        }

        try
        {
            var files = Directory.GetFiles(directoryPath);
            foreach (var file in files)
            {
                File.Delete(file);
            }
            _logger.LogInformation("All files deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting files: {ex.Message}");
        }
    }

    private async Task ExtractZipFromBytesAsync(string outputDirectory, byte[] zipData, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream(zipData);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, leaveOpen: false);

        foreach (var entry in archive.Entries)
        {
            string destinationPath = Path.Combine(outputDirectory, entry.FullName);

            string? directoryPath = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (!string.IsNullOrEmpty(entry.Name))
            {
                using var entryStream = entry.Open();
                using var outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);

                await entryStream.CopyToAsync(outputStream, cancellationToken);
            }
        }
    }
}