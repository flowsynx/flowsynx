using FlowSynx.IO;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Microsoft.Extensions.Logging;
using File = Google.Apis.Drive.v3.Data.File;

namespace FlowSynx.Plugin.Storage.Google.Drive;

internal class GoogleDriveBrowser : IDisposable
{
    private readonly ILogger _logger;
    private readonly DriveService _client;

    public GoogleDriveBrowser(ILogger logger, DriveService client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<IReadOnlyCollection<StorageEntity>> ListAsync(string folderId, string path,
        StorageSearchOptions searchOptions, StorageListOptions listOptions,
        StorageMetadataOptions metadataOptions, CancellationToken cancellationToken)
    {
        var entities = new List<StorageEntity>();

        await ListFolderAsync(entities, folderId, path, searchOptions, listOptions,
            metadataOptions, cancellationToken).ConfigureAwait(false);

        return entities;
    }
    
    private async Task ListFolderAsync(List<StorageEntity> entities, string folderId, string path,
        StorageSearchOptions searchOptions, StorageListOptions listOptions,
        StorageMetadataOptions metadataOptions, CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();

        var request = _client.Files.List();
        request.Q = $"'{folderId}' in parents";
        request.Fields = "nextPageToken, files(name, id, size, mimeType, modifiedTime, md5Checksum)";
        var folders = new List<File>();

        do
        {
            FileList files = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (files != null)
            {
                foreach (var item in files.Files)
                {
                    if (item == null)
                        continue;

                    if (item.MimeType == "application/vnd.google-apps.folder")
                    {
                        result.Add(item.ToEntity(true, metadataOptions.IncludeMetadata));
                        folders.Add(item);
                    }
                    else
                    {
                        result.Add(item.ToEntity(false, metadataOptions.IncludeMetadata));
                    }
                }
            }

            request.PageToken = files?.NextPageToken;
        }
        while (request.PageToken != null);

        entities.AddRange(result);

        if (searchOptions.Recurse)
        {
            //var directories = result.Where(b => b.Kind == StorageEntityItemKind.Directory).ToList();
            await Task.WhenAll(folders.Select(f => ListFolderAsync(entities, f.Id, f.Name,
                searchOptions, listOptions, metadataOptions, cancellationToken))).ConfigureAwait(false);
        }
    }

    //private string GetFolderId(string path)
    //{
    //    var pathParts = PathHelper.Split(path);

    //    var request = _client.Files.List();
    //    request.Q = $"mimeType = 'application/vnd.google-apps.folder' and name contains '{path}'";
    //    request.Fields = "nextPageToken, files(id, name)";

    //    IList<File> files = request.Execute().Files;
    //    if (files is { Count: > 0 })
    //    {
    //        foreach (var file in files)
    //        {   //My TextBlock(WPF)
    //            ListedFiles.Text = $"{file.Name}, {file.Id} \n";
    //        }
    //    }
    //}

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
