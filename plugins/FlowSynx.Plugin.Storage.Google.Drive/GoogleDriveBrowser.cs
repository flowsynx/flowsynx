﻿using FlowSynx.IO;
using FlowSynx.Plugin.Storage.Abstractions;
using FlowSynx.Plugin.Storage.Abstractions.Options;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Plugin.Storage.Google.Drive;

internal class GoogleDriveBrowser : IDisposable
{
    private readonly ILogger _logger;
    private readonly DriveService _client;
    private readonly string _rootFolderId;
    private readonly Dictionary<string, string> _pathDictionary;

    public GoogleDriveBrowser(ILogger logger, DriveService client, string rootFolderId)
    {
        _logger = logger;
        _client = client;
        _rootFolderId = rootFolderId;
        _pathDictionary = new Dictionary<string, string> { { "/", rootFolderId }, { "", rootFolderId } };
    }

    private IEnumerable<string> Fields => new List<string>()
    {
        "id",
        "name",
        "parents",
        "size",
        "mimeType",
        "createdTime",
        "modifiedTime",
        "md5Checksum",
        "copyRequiresWriterPermission",
        "description",
        "folderColorRgb",
        "starred",
        "viewedByMe",
        "viewedByMeTime",
        "writersCanShare"
    };

    public async Task<IReadOnlyCollection<StorageEntity>> ListAsync(string path,
        StorageSearchOptions searchOptions, StorageListOptions listOptions,
        StorageMetadataOptions metadataOptions, CancellationToken cancellationToken)
    {
        var entities = new List<StorageEntity>();

        await ListFolderAsync(entities, path, searchOptions, listOptions,
            metadataOptions, cancellationToken).ConfigureAwait(false);

        return entities;
    }
    
    private async Task ListFolderAsync(List<StorageEntity> entities, string path,
        StorageSearchOptions searchOptions, StorageListOptions listOptions,
        StorageMetadataOptions metadataOptions, CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();
        var folderId = await GetFolderId(path, cancellationToken);
        var request = _client.Files.List();
        request.Q = $"'{folderId}' in parents and (trashed=false)";
        request.Fields = $"nextPageToken, files({string.Join(",", Fields)})";

        do
        {
            var files = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (files != null)
            {
                foreach (var item in files.Files)
                {
                    if (item == null)
                        continue;

                    result.Add(item.MimeType == "application/vnd.google-apps.folder"
                        ? item.ToEntity(path, true, metadataOptions.IncludeMetadata)
                        : item.ToEntity(path, false, metadataOptions.IncludeMetadata));
                }
            }

            request.PageToken = files?.NextPageToken;
        }
        while (request.PageToken != null);

        entities.AddRange(result);

        if (searchOptions.Recurse)
        {
            var directories = result.Where(b => b.Kind == StorageEntityItemKind.Directory).ToList();
            await Task.WhenAll(directories.Select(dir => ListFolderAsync(entities, dir.FullPath,
                searchOptions, listOptions, metadataOptions, cancellationToken))).ConfigureAwait(false);
        }
    }
    
    public async Task<string> GetFolderId(string path, CancellationToken cancellationToken)
    {
        if (_pathDictionary.ContainsKey(path))
            return _pathDictionary[path];

        var folderId = _rootFolderId;
        var route = string.Empty;
        var queue = new Queue<string>();
        var pathParts = PathHelper.Split(path);
        foreach (var subPath in pathParts)
        {
            queue.Enqueue(subPath);
        }

        while (queue.Count > 0)
        {
            var enqueuePath = queue.Dequeue();
            var request = _client.Files.List();

            request.Q = $"('{folderId}' in parents) " +
                        $"and (mimeType = 'application/vnd.google-apps.folder') " +
                        $"and (name = '{enqueuePath}') " +
                        $"and (trashed=false)";

            request.Fields = "nextPageToken, files(id, name)";

            var fileListResponse = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (fileListResponse is null || fileListResponse.Files.Count <= 0)
            {
                _logger.LogWarning("The entered path could not be found!");
                return string.Empty;
            }

            var file = fileListResponse.Files.First();
            folderId = file.Id;
            route = PathHelper.Combine(route, file.Name);

            if (!_pathDictionary.ContainsKey(route))
                _pathDictionary.Add(route, file.Id);
        }

        return folderId;
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

    public void Dispose() { }
}
