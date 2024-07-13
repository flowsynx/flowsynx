using FlowSynx.IO;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Plugin.Storage.Google.Cloud;

internal class GoogleBucketBrowser : IDisposable
{
    private readonly ILogger _logger;
    private readonly StorageClient _client;
    private readonly string _bucketName;

    public GoogleBucketBrowser(ILogger logger, StorageClient client, string bucketName)
    {
        _logger = logger;
        _client = client;
        _bucketName = bucketName;
    }

    public async Task<IReadOnlyCollection<StorageEntity>> ListFolderAsync(string path, 
        StorageSearchOptions searchOptions, StorageListOptions listOptions,
        StorageMetadataOptions metadataOptions, CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();

        try
        {
            var request = _client.Service.Objects.List(_bucketName);
            request.Prefix = FormatFolderPrefix(path);
            request.Delimiter = searchOptions.Recurse ? null : "/";
            
            do
            {
                var serviceObjects = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                if (serviceObjects.Items != null)
                {
                    foreach (var item in serviceObjects.Items)
                    {
                        if (item == null)
                            continue;

                        result.Add(item.Name.EndsWith("/")
                            ? item.ToEntity(true, metadataOptions.IncludeMetadata)
                            : item.ToEntity(false, metadataOptions.IncludeMetadata));
                    }
                }

                if (serviceObjects.Prefixes != null)
                    result.AddRange(serviceObjects.Prefixes.Select(p => new StorageEntity(p, StorageEntityItemKind.Directory)));
                
                request.PageToken = serviceObjects.NextPageToken;
            }
            while (request.PageToken != null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        return result;
    }
    
    private string? FormatFolderPrefix(string folderPath)
    {
        folderPath = PathHelper.Normalize(folderPath);

        if (PathHelper.IsRootPath(folderPath))
            return null;

        if (!folderPath.EndsWith("/"))
            folderPath += "/";

        return folderPath;
    }

    public void Dispose()
    {
    }
}
