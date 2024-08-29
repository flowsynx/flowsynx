using EnsureThat;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google;
using System.Net;
using FlowSynx.IO;
using FlowSynx.Net;
using FlowSynx.Plugin.Abstractions.Extensions;
using Google.Apis.Upload;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Plugin.Storage.Filters;

namespace FlowSynx.Plugin.Storage.Google.Drive;

public class GoogleDriveStorage : IPlugin
{
    private readonly ILogger<GoogleDriveStorage> _logger;
    private readonly IStorageFilter _storageFilter;
    private readonly ISerializer _serializer;
    private GoogleDriveSpecifications? _googleDriveSpecifications;
    private DriveService _client = null!;
    private GoogleDriveBrowser? _browser;

    public GoogleDriveStorage(ILogger<GoogleDriveStorage> logger, IStorageFilter storageFilter, ISerializer serializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        _logger = logger;
        _storageFilter = storageFilter;
        _serializer = serializer;
    }
    
    public Guid Id => Guid.Parse("359e62f0-8ccf-41c4-a1f5-4e34d6790e84");
    public string Name => "Google.Drive";
    public PluginNamespace Namespace => PluginNamespace.Storage;
    public string? Description => Resources.PluginDescription;
    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(GoogleDriveSpecifications);

    public Task Initialize()
    {
        _googleDriveSpecifications = Specifications.ToObject<GoogleDriveSpecifications>();
        _client = CreateClient(_googleDriveSpecifications);
        return Task.CompletedTask;
    }

    public async Task<object> About(PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var aboutFilters = filters.ToObject<AboutFilters>();
        long totalSpace = 0, totalUsed = 0, totalFree = 0;
        try
        {
            var request = _client.About.Get();
            request.Fields = "storageQuota";
            var response = await request.ExecuteAsync(cancellationToken);
            totalUsed = response.StorageQuota.UsageInDrive ?? 0;
            if (response.StorageQuota.Limit is > 0)
            {
                totalSpace = response.StorageQuota.Limit.Value;
                totalFree = totalSpace - totalUsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            totalSpace = 0;
            totalUsed = 0;
            totalFree = 0;
        }

        return new { 
            Total = totalSpace.ToString(!aboutFilters.Full), 
            Free = totalFree.ToString(!aboutFilters.Full), 
            Used = totalUsed.ToString(!aboutFilters.Full)
        };
    }

    public async Task<object> CreateAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var createFilters = filters.ToObject<CreateFilters>();

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
            Parents = new string[] { folder.Id }
        };

        var command = _client.Files.Create(driveFolder);
        var file = await command.ExecuteAsync(cancellationToken);
        _logger.LogInformation($"Directory '{folderName}' was created successfully.");

        var result = new StorageEntity(path, StorageEntityItemKind.Directory);
        return new { result.Id };
    }

    public async Task<object> WriteAsync(string entity, PluginFilters? filters, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var writeFilters = filters.ToObject<WriteFilters>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        if (dataOptions is not Stream dataStream)
            throw new StorageException(nameof(dataStream));

        try
        {
            var file = await GetDriveFile(path, cancellationToken);
            if (file.Exist && writeFilters.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            var fileName = Path.GetFileName(path) ?? "";
            if (string.IsNullOrEmpty(fileName))
                throw new StorageException(string.Format(Resources.TePathIsNotFile, path));

            var directoryPath = PathHelper.GetParent(path) ?? "";
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

            var result = new StorageEntity(path, StorageEntityItemKind.File);
            return new { result.Id };
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task<object> ReadAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var readFilters = filters.ToObject<ReadFilters>();

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
            var fileExtension = Path.GetExtension(path);

            ms.Position = 0;

            return new StorageRead()
            {
                Stream = new StorageStream(ms),
                ContentType = fileRequest.MimeType,
                Extension = fileExtension,
                Md5 = fileRequest.Md5Checksum,
            };
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public Task<object> UpdateAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<object>> DeleteAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var deleteFilters = filters.ToObject<DeleteFilters>();
        var entities = await ListAsync(path, filters, cancellationToken).ConfigureAwait(false);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var result = new List<string>();
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageList list)
                continue;

            if (await DeleteEntityAsync(list.Path, cancellationToken).ConfigureAwait(false))
            {
                result.Add(list.Id);
            }
        }

        if (deleteFilters.Purge is true)
        {
            var folder = await GetDriveFolder(path, cancellationToken).ConfigureAwait(false);
            if (folder.Exist)
            {
                if (!PathHelper.IsRootPath(path))
                {
                    await _client.Files.Delete(folder.Id).ExecuteAsync(cancellationToken);
                    _browser?.DeleteFolderId(path);
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

        return result;
    }

    public async Task<bool> ExistAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
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

    public async Task<IEnumerable<object>> ListAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);

        if (_googleDriveSpecifications == null)
            throw new StorageException(Resources.SpecificationsCouldNotBeNullOrEmpty);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var listFilters = filters.ToObject<ListFilters>();

        var storageEntities = await ListAsync(_googleDriveSpecifications.FolderId, path,
            listFilters, cancellationToken).ConfigureAwait(false);

        var filteredEntities = _storageFilter.Filter(storageEntities, filters).ToList();

        var result = new List<StorageList>(filteredEntities.Count());
        result.AddRange(filteredEntities.Select(storageEntity => new StorageList
        {
            Id = storageEntity.Id,
            Kind = storageEntity.Kind.ToString().ToLower(),
            Name = storageEntity.Name,
            Path = storageEntity.FullPath,
            CreatedTime = storageEntity.CreatedTime,
            ModifiedTime = storageEntity.ModifiedTime,
            Size = storageEntity.Size.ToString(!listFilters.Full),
            ContentType = storageEntity.ContentType,
            Md5 = storageEntity.Md5,
            Metadata = storageEntity.Metadata
        }));

        return result;
    }
    
    public void Dispose() { }

    #region private methods
    private DriveService CreateClient(GoogleDriveSpecifications specifications)
    {
        var jsonObject = new
        {
            type = specifications.Type,
            project_id = specifications.ProjectId,
            private_key_id = specifications.PrivateKeyId,
            private_key = specifications.PrivateKey,
            client_email = specifications.ClientEmail,
            client_id = specifications.ClientId,
            auth_uri = specifications.AuthUri,
            token_uri = specifications.TokenUri,
            auth_provider_x509_cert_url = specifications.AuthProviderX509CertUrl,
            client_x509_cert_url = specifications.ClientX509CertUrl,
            universe_domain = specifications.UniverseDomain,
        };

        var json = _serializer.Serialize(jsonObject);
        var credential = GoogleCredential.FromJson(json);

        if (credential == null)
            throw new StorageException(Resources.ErrorInCreateDriveServiceCredential);

        if (credential.IsCreateScopedRequired)
        {
            string[] scopes = { 
                DriveService.Scope.Drive, 
                DriveService.Scope.DriveMetadataReadonly,
                DriveService.Scope.DriveFile,
            };
            credential = credential.CreateScoped(scopes);
        }

        var driveService = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });

        return driveService;
    }

    private async Task<List<StorageEntity>> ListAsync(string folderId, string path,
        ListFilters listFilters, CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();
        _browser ??= new GoogleDriveBrowser(_logger, _client, folderId);
        IReadOnlyCollection<StorageEntity> objects =
            await _browser.ListAsync(path, listFilters, cancellationToken
        ).ConfigureAwait(false);

        if (objects.Count > 0)
        {
            result.AddRange(objects);
        }
        return result;
    }

    private async Task<GoogleDrivePath> GetDriveFile(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(fileName))
                throw new StorageException(string.Format(Resources.TePathIsNotFile, filePath));

            var directoryPath = PathHelper.GetParent(filePath) + PathHelper.PathSeparatorString ?? "";
            var folderId = await GetFolderId(directoryPath, cancellationToken).ConfigureAwait(false);

            var listRequest = _client.Files.List();
            listRequest.Q = $"('{folderId}' in parents) and (name='{fileName}') and (trashed=false)";
            var files = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return !files.Files.Any()
                ? new GoogleDrivePath(false, string.Empty)
                : new GoogleDrivePath(true, files.Files.First().Id);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return new GoogleDrivePath(false, string.Empty);
        }
    }

    private async Task<GoogleDrivePath> GetDriveFolder(string path, CancellationToken cancellationToken)
    {
        try
        {
            var folderId = await GetFolderId(path, cancellationToken).ConfigureAwait(false);
            return new GoogleDrivePath(!string.IsNullOrEmpty(folderId), folderId);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return new GoogleDrivePath(false, string.Empty);
        }
    }

    private async Task<string> GetFolderId(string folderPath, CancellationToken cancellationToken)
    {
        var rootFolderId = GetRootFolderId();
        _browser ??= new GoogleDriveBrowser(_logger, _client, rootFolderId);
        return await _browser.GetFolderId(folderPath, cancellationToken).ConfigureAwait(false);
    }

    private string GetRootFolderId()
    {
        if (_googleDriveSpecifications == null)
            throw new StorageException(Resources.SpecificationsCouldNotBeNullOrEmpty);

        return _googleDriveSpecifications.FolderId;
    }

    private async Task<bool> DeleteEntityAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            if (PathHelper.IsFile(path))
            {
                var file = await GetDriveFile(path, cancellationToken).ConfigureAwait(false);
                if (!file.Exist)
                    return false;

                var command = _client.Files.Delete(file.Id);
                await command.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }

            var folder = await GetDriveFolder(path, cancellationToken).ConfigureAwait(false);
            if (!folder.Exist)
                return false;

            if (PathHelper.IsRootPath(path))
                throw new StorageException("Can't purge root directory");

            await _client.Files.Delete(folder.Id).ExecuteAsync(cancellationToken);
            _browser?.DeleteFolderId(path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex.Message);
            return false;
        }
    }
    #endregion
}