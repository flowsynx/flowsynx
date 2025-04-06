using FlowSynx.Application.Services;
using Microsoft.Extensions.Logging;

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
}