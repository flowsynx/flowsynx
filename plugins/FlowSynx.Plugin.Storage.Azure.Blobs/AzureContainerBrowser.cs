using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using FlowSynx.IO;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Plugin.Storage.Azure.Blobs;

internal class AzureContainerBrowser : IDisposable
{
    private readonly ILogger _logger;
    private readonly BlobContainerClient _client;

    public AzureContainerBrowser(ILogger logger, BlobContainerClient client)
    {
        _logger = logger;
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<IReadOnlyCollection<StorageEntity>> ListFolderAsync(string path, 
        StorageSearchOptions searchOptions, StorageListOptions listOptions,
        CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();
        var tempResult = new List<StorageEntity>();

        try
        {
            var blobs = _client.GetBlobsByHierarchyAsync(
                delimiter: searchOptions.Recurse ? null : "/",
                prefix: FormatFolderPrefix(path),
                traits: BlobTraits.Metadata,
                states: BlobStates.None
            ).ConfigureAwait(false);

            await foreach (BlobHierarchyItem item in blobs)
            {
                try
                {
                    if (item.IsBlob)
                    {
                        var entity = AzureBlobConverter.ToEntity(containerName: _client.Name, item: item);

                        if (listOptions.Kind is StorageFilterItemKind.File or StorageFilterItemKind.FileAndDirectory)
                            result.Add(entity);

                        tempResult.Add(entity);
                    }

                    if ((listOptions.Kind is StorageFilterItemKind.Directory or StorageFilterItemKind.FileAndDirectory) && item.IsPrefix)
                    {
                        result.Add(AzureBlobConverter.ToEntity(containerName: _client.Name, prefix: item.Prefix));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }

            if (searchOptions.Recurse && (listOptions.Kind is StorageFilterItemKind.Directory or StorageFilterItemKind.FileAndDirectory))
            {
                var implicitPrefixes = AssumeImplicitPrefixes(
                    PathHelper.Combine(_client.Name, path),
                    tempResult);

                if (implicitPrefixes.Count > 0)
                {
                    result.AddRange(implicitPrefixes);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        
        return result;
    }

    private IReadOnlyCollection<StorageEntity> AssumeImplicitPrefixes(string absoluteRoot, IEnumerable<StorageEntity> blobs)
    {
        var result = new List<StorageEntity>();
        absoluteRoot = PathHelper.Normalize(absoluteRoot);

        List<StorageEntity> implicitFolders = blobs
           .Select(b => b.FullPath)
           .Select(PathHelper.GetParent)
           .Where(p => !PathHelper.IsRootPath(p))
           .Distinct()
           .Select(p => new StorageEntity(p, StorageEntityItemKind.Directory))
           .ToList();

        result.AddRange(implicitFolders);
        return result;
    }

    private string FormatFolderPrefix(string folderPath)
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
