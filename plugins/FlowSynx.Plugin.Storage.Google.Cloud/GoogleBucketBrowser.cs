using FlowSynx.IO;
using FlowSynx.Plugin.Storage.Filters;
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
        ListFilters listFilters, CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();

        try
        {
            var request = _client.Service.Objects.List(_bucketName);
            request.Prefix = FormatFolderPrefix(path);
            request.Delimiter = listFilters.Recurse ? null : PathHelper.PathSeparatorString;
            
            do
            {
                var serviceObjects = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                if (serviceObjects.Items != null)
                {
                    foreach (var item in serviceObjects.Items)
                    {
                        if (item == null)
                            continue;

                        result.Add(item.Name.EndsWith(PathHelper.PathSeparator)
                            ? item.ToEntity(true, listFilters.IncludeMetadata)
                            : item.ToEntity(false, listFilters.IncludeMetadata));
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

        if (!folderPath.EndsWith(PathHelper.PathSeparator))
            folderPath += PathHelper.PathSeparator;

        return folderPath;
    }

    public void Dispose()
    {
    }
}
