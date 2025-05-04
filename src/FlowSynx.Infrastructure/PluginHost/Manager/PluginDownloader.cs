using FlowSynx.Application.Models;
using FlowSynx.Application.Serialization;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Security.Cryptography;

namespace FlowSynx.Infrastructure.PluginHost.Manager;

public class PluginDownloader : IPluginDownloader
{
    private readonly ILogger<PluginDownloader> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJsonDeserializer _jsonDeserializer;

    public PluginDownloader(
        ILogger<PluginDownloader> logger, 
        IHttpClientFactory httpClientFactory,
        IJsonDeserializer jsonDeserializer)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(jsonDeserializer);
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _jsonDeserializer = jsonDeserializer;
    }

    public async Task<byte[]> GetPluginDataAsync(string url)
    {
        var client = _httpClientFactory.CreateClient("PluginRegistry");
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsByteArrayAsync();

        var message = string.Format(Resources.Plugin_Download_FailedToFetchDataFromUrl, (int)response.StatusCode, response.ReasonPhrase);
        throw new FlowSynxException((int)ErrorCode.PluginRegistryFailedToFetchDataFromUrl, message);
    }

    public async Task<PluginInstallMetadata> GetPluginMetadataAsync(string url, string pluginType, string pluginVersion)
    {
        var client = _httpClientFactory.CreateClient("PluginRegistry");
        var mainUrl = new Uri(url);
        var indexUrl = new Uri(mainUrl, "index.json");
        var response = await client.GetStringAsync(indexUrl);
        var plugins = _jsonDeserializer.Deserialize<List<PluginInstallMetadata>>(response);
        var metadata = plugins
            .FirstOrDefault(x => 
                string.Equals(x.Type, pluginType, StringComparison.CurrentCultureIgnoreCase) && x.Version == pluginVersion);

        if (metadata != null) 
            return metadata;

        var message = string.Format(Resources.Plugin_Download_PluginNotFound, pluginType, pluginVersion);
        throw new FlowSynxException((int)ErrorCode.PluginRegistryPluginNotFound, message);

    }

    public async Task ExtractPluginAsync(string pluginDirectory, byte[] data, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => DeleteAllFiles(pluginDirectory), cancellationToken);
            await ExtractZipFromBytesAsync(pluginDirectory, data, cancellationToken);
            _logger.LogInformation(string.Format(Resources.Plugin_Download_Extraction_Successfully, pluginDirectory));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(Resources.PluginDownloader_ErrorInExtractingPackage, ex.Message), ex);
        }
    }

    public bool ValidateChecksum(byte[] data, string expectedChecksum)
    {
        var computedChecksum = ComputeChecksum(data);
        return computedChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    private string ComputeChecksum(byte[] data)
    {
        using SHA256 sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private void DeleteAllFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            _logger.LogDebug(string.Format(Resources.Plugin_Download_Extraction_DirectoryNotFound, directoryPath));
            return;
        }

        try
        {
            var files = Directory.GetFiles(directoryPath);
            foreach (var file in files)
            {
                File.Delete(file);
            }
            _logger.LogInformation(Resources.Plugin_Download_Extraction_AllFilesDeletedSuccessfully);
        }
        catch (Exception ex)
        {
            _logger.LogError(string.Format(Resources.Plugin_Download_Extraction_ErrorDuringDelete, ex.Message));
        }
    }

    private async Task ExtractZipFromBytesAsync(string outputDirectory, byte[] zipData, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream(zipData);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, leaveOpen: false);

        foreach (var entry in archive.Entries)
        {
            var destinationPath = Path.Combine(outputDirectory, entry.FullName);

            var directoryPath = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (string.IsNullOrEmpty(entry.Name)) 
                continue;

            await using var entryStream = entry.Open();
            await using var outputStream = new FileStream(
                path: destinationPath, 
                mode: FileMode.Create, 
                access: FileAccess.Write, 
                share: FileShare.None, 
                bufferSize: 8192, 
                useAsync: true);

            await entryStream.CopyToAsync(outputStream, cancellationToken);
        }
    }
}