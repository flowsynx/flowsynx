using EnsureThat;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Reflections;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google;
using System.Net;
using FlowSynx.IO;
using FlowSynx.Net;
using Google.Apis.Upload;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using System.IO;


namespace FlowSynx.Plugin.Storage.Google.Drive;

public class GoogleDriveStorage : IStoragePlugin
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
    public Dictionary<string, string?>? Specifications { get; set; }
    public Type SpecificationsType => typeof(GoogleDriveSpecifications);

    public Task Initialize()
    {
        _googleDriveSpecifications = Specifications.DictionaryToObject<GoogleDriveSpecifications>();
        _client = CreateClient(_googleDriveSpecifications);
        return Task.CompletedTask;
    }

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
            string[] scopes = { DriveService.Scope.Drive, DriveService.Scope.DriveMetadataReadonly };
            credential = credential.CreateScoped(scopes);
        }

        var driveService = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });

        return driveService;

    }

    public async Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
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

        return new StorageUsage { Total = totalSpace, Free = totalFree, Used = totalUsed };
    }

    public async Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, StorageMetadataOptions metadataOptions,
        CancellationToken cancellationToken = default)
    {
        if (_googleDriveSpecifications == null)
            throw new StorageException(Resources.SpecificationsCouldNotBeNullOrEmpty);

        var result = await ListAsync(_googleDriveSpecifications.FolderId, path, 
            searchOptions, listOptions, metadataOptions, cancellationToken).ConfigureAwait(false);

        var filteredResult = _storageFilter.FilterEntitiesList(result, searchOptions, listOptions);

        if (listOptions.MaxResult is > 0)
            filteredResult = filteredResult.Take(listOptions.MaxResult.Value);

        return filteredResult;
    }
    
    public async Task WriteAsync(string path, StorageStream dataStream, StorageWriteOptions writeOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (dataStream == null)
            throw new ArgumentNullException(nameof(dataStream));

        try
        {
            var file = await GetDriveFile(path, cancellationToken);
            if (file.Exist && writeOptions.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            var fileName = Path.GetFileName(path) ?? "";
            if (string.IsNullOrEmpty(fileName))
                throw new StorageException(string.Format(Resources.TePathIsNotFile, path));

            var directoryPath = Path.GetDirectoryName(path) ?? "";
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
    
    public async Task<StorageRead> ReadAsync(string path, StorageHashOptions hashOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

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

    public async Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var result = await GetDriveFile(path, cancellationToken).ConfigureAwait(false);
            return result.Exist;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        var listOptions = new StorageListOptions { Kind = StorageFilterItemKind.File };
        var hashOptions = new StorageHashOptions() { Hashing = false };
        var metadataOptions = new StorageMetadataOptions() { IncludeMetadata = false };

        var entities =
            await ListAsync(path, storageSearches, listOptions, hashOptions, metadataOptions, cancellationToken)
                .ConfigureAwait(false);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            _logger.LogWarning(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        foreach (var entity in storageEntities)
        {
            await DeleteFileAsync(entity.FullPath, cancellationToken);
        }
    }

    public async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var file = await GetDriveFile(path, cancellationToken).ConfigureAwait(false);
            if (!file.Exist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var command = _client.Files.Delete(file.Id);
            await command.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        var pathParts = PathHelper.Split(path);
        var folderName = pathParts.Last();
        var parentFolder = string.Join(PathHelper.PathSeparatorString, pathParts.SkipLast(1));
        var folder = await GetDriveFolder(parentFolder, cancellationToken).ConfigureAwait(false);
        if (folder.Exist)
        {
            var driveFolder = new DriveFile
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new string[] { folder.Id }
            };

            var command = _client.Files.Create(driveFolder);
            var file = await command.ExecuteAsync(cancellationToken);
            _logger.LogInformation($"Directory '{folderName}' was created successfully.");
        }
        else
        {
            throw new StorageException($"The Entered path '{parentFolder}' is not exist.");
        }
    }

    public async Task PurgeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var folder = await GetDriveFolder(path, cancellationToken).ConfigureAwait(false);
            if (!folder.Exist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var searchOptions = new StorageSearchOptions();
            await DeleteAsync(path, searchOptions, cancellationToken);

            if (PathHelper.IsRootPath(path))
                throw new StorageException("Can't purge root directory");

            var command = _client.Files.Delete(folder.Id);
            await command.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public async Task<bool> DirectoryExistAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var result = await GetDriveFolder(path, cancellationToken).ConfigureAwait(false);
            return result.Exist;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public void Dispose() { }

    #region private methods
    private async Task<List<StorageEntity>> ListAsync(string folderId, string path,
        StorageSearchOptions searchOptions, StorageListOptions listOptions, 
        StorageMetadataOptions metadataOptions, CancellationToken cancellationToken)
    {
        var result = new List<StorageEntity>();
        _browser ??= new GoogleDriveBrowser(_logger, _client, folderId);
        IReadOnlyCollection<StorageEntity> objects =
            await _browser.ListAsync(path, searchOptions, listOptions, metadataOptions, cancellationToken
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

            var directoryPath = Path.GetDirectoryName(filePath) ?? "";
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

    //private async Task<bool> BucketExists(string bucketName, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        await _client.GetBucketAsync(bucketName, null, cancellationToken).ConfigureAwait(false);
    //        return true;
    //    }
    //    catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
    //    {
    //        return false;
    //    }
    //}

    //private async Task AddFolder(string bucketName, string folderName, CancellationToken cancellationToken)
    //{
    //    if (!folderName.EndsWith("/"))
    //        folderName += "/";

    //    var content = Encoding.UTF8.GetBytes("");
    //    await _client.UploadObjectAsync(bucketName, folderName, "application/x-directory", 
    //        new MemoryStream(content), cancellationToken: cancellationToken);
    //    _logger.LogInformation($"Folder '{folderName}' was created successfully.");
    //}
    #endregion
}