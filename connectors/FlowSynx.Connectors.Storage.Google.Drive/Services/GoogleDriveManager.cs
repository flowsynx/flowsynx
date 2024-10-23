using FlowSynx.IO;
using FlowSynx.Connectors.Storage.Options;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.Logging;
using FlowSynx.Connectors.Storage.Google.Drive.Extensions;
using FlowSynx.Connectors.Storage.Exceptions;
using FlowSynx.Connectors.Storage.Google.Drive.Models;
using Google;
using System.Net;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Net;
using Google.Apis.Upload;
using FlowSynx.Connectors.Abstractions;
using EnsureThat;

namespace FlowSynx.Connectors.Storage.Google.Drive.Services;

internal class GoogleDriveManager : IGoogleDriveManager, IDisposable
{
    private readonly ILogger _logger;
    private readonly DriveService _client;
    private readonly string _rootFolderId;
    private readonly Dictionary<string, string> _pathDictionary;
    private readonly GoogleDriveSpecifications? _specifications;

    public GoogleDriveManager(ILogger logger, DriveService client, GoogleDriveSpecifications? specifications)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(specifications, nameof(specifications));
        _logger = logger;
        _client = client;
        _specifications = specifications;
        _rootFolderId = specifications.FolderId;
        _pathDictionary = new Dictionary<string, string> { { PathHelper.PathSeparatorString, _rootFolderId }, { "", _rootFolderId } };
    }

    public async Task CreateAsync(string entity, CreateOptions options, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var pathParts = PathHelper.Split(path);
        var folderName = pathParts.Last();
        var parentFolder = string.Join(PathHelper.PathSeparatorString, pathParts.SkipLast(1));
        var folder = await GetDriveFolder(parentFolder, cancellationToken).ConfigureAwait(false);

        if (!folder.Exist)
            throw new StorageException(string.Format(Resources.ParentPathIsNotExist, parentFolder));

        var driveFolder = new DriveFile
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new[] { folder.Id }
        };

        var command = _client.Files.Create(driveFolder);
        await command.ExecuteAsync(cancellationToken);
        _logger.LogInformation($"Directory '{folderName}' was created successfully.");
    }

    public async Task WriteAsync(string entity, WriteOptions options, object dataOptions, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        var dataValue = dataOptions.GetObjectValue();
        if (dataValue is not string data)
            throw new StorageException(Resources.EnteredDataIsNotValid);

        var dataStream = data.IsBase64String() ? data.Base64ToStream() : data.ToStream();

        try
        {
            var file = await GetDriveFile(path, cancellationToken);
            if (file.Exist && options.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            var fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName))
                throw new StorageException(string.Format(Resources.TePathIsNotFile, path));

            var directoryPath = PathHelper.GetParent(path);
            var folderId = await GetFolderId(directoryPath, cancellationToken).ConfigureAwait(false);

            var fileMime = Path.GetExtension(fileName).GetContentType();
            var driveFile = new DriveFile
            {
                Name = fileName,
                MimeType = fileMime,
                Parents = new[] { folderId }
            };

            var request = _client.Files.Create(driveFile, dataStream, fileMime);
            var response = await request.UploadAsync(cancellationToken).ConfigureAwait(false);
            if (response.Status != UploadStatus.Completed)
                throw response.Exception;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task<ReadResult> ReadAsync(string entity, ReadOptions options, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var file = await GetDriveFile(path, cancellationToken);
            if (!file.Exist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var ms = new MemoryStream();
            var request = _client.Files.Get(file.Id);
            request.Fields = "id, name, mimeType, md5Checksum";
            var fileRequest = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            request.Download(ms);

            ms.Position = 0;

            return new ReadResult
            {
                Content = ms.ToArray(),
                ContentHash = fileRequest.Md5Checksum,
            };
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task DeleteAsync(string entity, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            if (PathHelper.IsFile(path))
            {
                var file = await GetDriveFile(path, cancellationToken).ConfigureAwait(false);
                if (!file.Exist)
                {
                    _logger.LogWarning(string.Format(Resources.TheSpecifiedPathIsNotExist, path));
                    return;
                }

                var command = _client.Files.Delete(file.Id);
                await command.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
                return;
            }

            var folder = await GetDriveFolder(path, cancellationToken).ConfigureAwait(false);
            if (!folder.Exist) return;

            if (PathHelper.IsRootPath(path))
                throw new StorageException(Resources.CanNotPurgeRootDirectory);

            await _client.Files.Delete(folder.Id).ExecuteAsync(cancellationToken);
            DeleteFolderId(path);
            _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex.Message);
        }
    }

    public async Task PurgeAsync(string entity, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        var folder = await GetDriveFolder(path, cancellationToken).ConfigureAwait(false);
        if (folder.Exist)
        {
            if (!PathHelper.IsRootPath(path))
            {
                await _client.Files.Delete(folder.Id).ExecuteAsync(cancellationToken);
                DeleteFolderId(path);
            }
            else
            {
                _logger.LogWarning($"The path {path} is root path and can't be purged!");
            }
        }
        else
        {
            _logger.LogWarning($"The path {path} is not exist!");
        }
    }

    public async Task<bool> ExistAsync(string entity, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            if (PathHelper.IsFile(path))
            {
                var fileExist = await GetDriveFile(path, cancellationToken).ConfigureAwait(false);
                return fileExist.Exist;
            }

            var folderExist = await GetDriveFolder(path, cancellationToken).ConfigureAwait(false);
            return folderExist.Exist;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task<IEnumerable<StorageEntity>> ListAsync(string entity, ListOptions listOptions,
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);

        if (_specifications == null)
            throw new StorageException(Resources.SpecificationsCouldNotBeNullOrEmpty);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = await ListObjectsAsync(path, listOptions, cancellationToken).ConfigureAwait(false);

        return storageEntities;
    }

    public void Dispose() { }

    #region internal methods

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

    private async Task<GoogleDrivePathPart> GetDriveFolder(string path, CancellationToken cancellationToken)
    {
        try
        {
            var folderId = await GetFolderId(path, cancellationToken).ConfigureAwait(false);
            return new GoogleDrivePathPart(!string.IsNullOrEmpty(folderId), folderId);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return new GoogleDrivePathPart(false, string.Empty);
        }
    }

    private async Task<GoogleDrivePathPart> GetDriveFile(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(fileName))
                throw new StorageException(string.Format(Resources.TePathIsNotFile, filePath));

            var directoryPath = PathHelper.GetParent(filePath) + PathHelper.PathSeparatorString;
            var folderId = await GetFolderId(directoryPath, cancellationToken).ConfigureAwait(false);

            var listRequest = _client.Files.List();
            listRequest.Q = $"('{folderId}' in parents) and (name='{fileName}') and (trashed=false)";
            var files = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return !files.Files.Any()
                ? new GoogleDrivePathPart(false, string.Empty)
                : new GoogleDrivePathPart(true, files.Files.First().Id);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return new GoogleDrivePathPart(false, string.Empty);
        }
    }

    private async Task<List<StorageEntity>> ListObjectsAsync(string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();
        await ListFolderAsync(result, path, listOptions, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task ListFolderAsync(List<StorageEntity> entities, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
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
                        ? item.ToEntity(path, true, listOptions.IncludeMetadata)
                        : item.ToEntity(path, false, listOptions.IncludeMetadata));
                }
            }

            request.PageToken = files?.NextPageToken;
        }
        while (request.PageToken != null);

        entities.AddRange(result);

        if (listOptions.Recurse)
        {
            var directories = result.Where(b => b.Kind == StorageEntityItemKind.Directory).ToList();
            await Task.WhenAll(directories.Select(dir => ListFolderAsync(entities, dir.FullPath,
                listOptions, cancellationToken))).ConfigureAwait(false);
        }
    }

    private async Task<string> GetFolderId(string path, CancellationToken cancellationToken)
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

            if (!route.EndsWith(PathHelper.PathSeparator))
                route += PathHelper.PathSeparator;

            if (!_pathDictionary.ContainsKey(route))
                _pathDictionary.Add(route, file.Id);
        }

        return folderId;
    }

    private void DeleteFolderId(string path)
    {
        if (_pathDictionary.ContainsKey(path))
            _pathDictionary.Remove(path);
    }

    #endregion
}
