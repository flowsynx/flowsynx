using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.Serialization;
using FlowSynx.Domain.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Security.Cryptography;

namespace FlowSynx.Infrastructure.PluginHost.Manager;

public class PluginDownloader : IPluginDownloader
{
    private const string PluginRegistryClientName = "PluginRegistry";

    private readonly ILogger<PluginDownloader> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly ILocalization _localization;

    public PluginDownloader(
        ILogger<PluginDownloader> logger, 
        IHttpClientFactory httpClientFactory,
        IJsonDeserializer jsonDeserializer,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(jsonDeserializer);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _jsonDeserializer = jsonDeserializer;
        _localization = localization;
    }

    public async Task<byte[]> GetPluginDataAsync(
        string url, 
        string pluginType, 
        string pluginVersion, 
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(PluginRegistryClientName);
        var mainUrl = new Uri(url);
        var pluginUrl = new Uri(mainUrl, $"api/plugins/{pluginType}/{pluginVersion}/download");
        var response = await client.GetAsync(pluginUrl, cancellationToken);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);

        var message = _localization.Get("Plugin_Download_FailedToFetchDataFromUrl", (int)response.StatusCode, response.ReasonPhrase);
        throw new FlowSynxException((int)ErrorCode.PluginRegistryFailedToFetchDataFromUrl, message);
    }

    public async Task<PluginInstallMetadata> GetPluginMetadataAsync(
        string url, 
        string pluginType, 
        string pluginVersion, 
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(PluginRegistryClientName);
        var mainUrl = new Uri(url);
        var pluginUrl = new Uri(mainUrl, $"api/plugins/{pluginType}/{pluginVersion}");
        var response = await client.GetStringAsync(pluginUrl, cancellationToken);
        var metadata = _jsonDeserializer.Deserialize<Result<PluginInstallMetadata>>(response);

        if (metadata == null)
        {
            var message = _localization.Get("Plugin_Download_PluginNotFound", pluginType, pluginVersion);
            throw new FlowSynxException((int)ErrorCode.PluginRegistryPluginNotFound, message);
        }

        if (!metadata.Succeeded)
        {
            throw new FlowSynxException((int)ErrorCode.PluginInstall, 
                string.Join(Environment.NewLine, metadata.Messages));
        }

        return metadata.Data;
    }

    public async Task<IEnumerable<PluginVersion>> GetPluginVersionsAsync(
        string url, 
        string pluginType, 
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(PluginRegistryClientName);
        var mainUrl = new Uri(url);
        var pluginUrl = new Uri(mainUrl, $"api/plugins/{pluginType}/versions");
        var response = await client.GetStringAsync(pluginUrl, cancellationToken);
        var versions = _jsonDeserializer.Deserialize<Result<IEnumerable<PluginVersion>>>(response);
        if (versions == null)
        {
            var message = _localization.Get("Plugin_Download_PluginVersionsNotFound", pluginType);
            throw new FlowSynxException((int)ErrorCode.PluginRegistryPluginVersionsNotFound, message);
        }
        if (!versions.Succeeded)
        {
            throw new FlowSynxException((int)ErrorCode.PluginInstall, 
                string.Join(Environment.NewLine, versions.Messages));
        }
        return versions.Data;
    }

    public async Task ExtractPluginAsync(
        string pluginDirectory, 
        byte[] data, 
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(() => DeleteAllFiles(pluginDirectory), cancellationToken);
            await ExtractZipFromBytesAsync(pluginDirectory, data, cancellationToken);
            _logger.LogInformation(_localization.Get("Plugin_Download_Extraction_Successfully", pluginDirectory));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(_localization.Get("PluginDownloader_ErrorInExtractingPackage", ex.Message), ex);
        }
    }

    public bool ValidateChecksum(
        byte[] data, 
        string? expectedChecksum)
    {
        var computedChecksum = ComputeChecksum(data);
        return computedChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<RegistryPluginItem>> GetPluginsListAsync(
        string url,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(PluginRegistryClientName);
        var mainUrl = new Uri(url);
        var listUrl = new Uri(mainUrl, "api/plugins");
        var response = await client.GetStringAsync(listUrl, cancellationToken);

        var result = _jsonDeserializer.Deserialize<Result<IEnumerable<RegistryPluginItem>>>(response);
        if (result == null)
        {
            var message = _localization.Get("Plugin_Download_PluginNotFound", "all", "latest");
            throw new FlowSynxException((int)ErrorCode.PluginRegistryPluginNotFound, message);
        }

        if (!result.Succeeded)
        {
            throw new FlowSynxException((int)ErrorCode.PluginInstall,
                string.Join(Environment.NewLine, result.Messages));
        }

        return result.Data;
    }

    private static string ComputeChecksum(byte[] data)
    {
        using SHA256 sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private void DeleteAllFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            _logger.LogDebug(_localization.Get("Plugin_Download_Extraction_DirectoryNotFound", directoryPath));
            return;
        }

        try
        {
            var files = Directory.GetFiles(directoryPath);
            foreach (var file in files)
            {
                File.Delete(file);
            }
            _logger.LogInformation(_localization.Get("Plugin_Download_Extraction_AllFilesDeletedSuccessfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,_localization.Get("Plugin_Download_Extraction_ErrorDuringDelete", ex.Message));
        }
    }

    private static async Task ExtractZipFromBytesAsync(
        string outputDirectory, 
        byte[] zipData, 
        CancellationToken cancellationToken)
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