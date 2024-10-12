using Amazon.S3;
using Amazon.S3.Model;
using FlowSynx.IO;
using FlowSynx.Connectors.Storage.Options;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Connectors.Storage.Amazon.S3;

internal class AmazonS3BucketBrowser : IDisposable
{
    private readonly ILogger _logger;
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;

    public AmazonS3BucketBrowser(ILogger logger, AmazonS3Client client, string bucketName)
    {
        _logger = logger;
        _client = client;
        _bucketName = bucketName;
    }

    public async Task<IReadOnlyCollection<StorageEntity>> ListAsync(string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        var entities = new List<StorageEntity>();
        await ListFolderAsync(entities, path, listOptions, cancellationToken).ConfigureAwait(false);
        return entities;
    }

    private async Task ListFolderAsync(List<StorageEntity> entities, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        var request = new ListObjectsV2Request()
        {
            BucketName = _bucketName,
            Prefix = FormatFolderPrefix(path),
            Delimiter = PathHelper.PathSeparatorString
        };

        var result = new List<StorageEntity>();
        do
        {
            var response = await _client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false);
            result.AddRange(response.ToEntity(_client, _bucketName, listOptions.IncludeMetadata, cancellationToken));

            if (response.NextContinuationToken == null)
                break;

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken != null);

        entities.AddRange(result);

        if (listOptions.Recurse)
        {
            var directories = result.Where(b => b.Kind == StorageEntityItemKind.Directory).ToList();
            await Task.WhenAll(directories.Select(f => ListFolderAsync(entities, GetRelativePath(f.FullPath),
                listOptions, cancellationToken))).ConfigureAwait(false);
        }
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

    private string GetRelativePath(string fullPath)
    {
        fullPath = PathHelper.Normalize(fullPath);
        string[] parts = PathHelper.Split(fullPath);
        return parts.Length == 1 ? string.Empty : PathHelper.Combine(parts.Skip(1));
    }

    public void Dispose() { }
}