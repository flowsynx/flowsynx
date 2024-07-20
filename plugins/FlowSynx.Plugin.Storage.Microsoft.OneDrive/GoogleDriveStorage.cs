using EnsureThat;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Reflections;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

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
            throw new StorageException("An error in creating Drive service!");

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

    public Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
        throw new StorageException(Resources.AboutOperrationNotSupported);
    }

    public async Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, StorageMetadataOptions metadataOptions,
        CancellationToken cancellationToken = default)
    {
        var result = await ListAsync(_googleDriveSpecifications.FolderId, path, 
            searchOptions, listOptions, metadataOptions, cancellationToken);

        var filteredResult = _storageFilter.FilterEntitiesList(result, searchOptions, listOptions);

        if (listOptions.MaxResult is > 0)
            filteredResult = filteredResult.Take(listOptions.MaxResult.Value);

        return filteredResult;
    }
    
    public async Task WriteAsync(string path, StorageStream dataStream, StorageWriteOptions writeOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
    public async Task<StorageRead> ReadAsync(string path, StorageHashOptions hashOptions,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
    public async Task PurgeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DirectoryExistAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }

    #region private methods
    //private GoogleCloudStorageBucketPathPart GetPartsAsync(string fullPath)
    //{
    //    fullPath = PathHelper.Normalize(fullPath);
    //    if (fullPath == null)
    //        throw new ArgumentNullException(nameof(fullPath));

    //    string bucketName, relativePath;
    //    string[] parts = PathHelper.Split(fullPath);

    //    if (parts.Length == 1)
    //    {
    //        bucketName = parts[0];
    //        relativePath = string.Empty;
    //    }
    //    else
    //    {
    //        bucketName = parts[0];
    //        relativePath = PathHelper.Combine(parts.Skip(1));
    //    }

    //    return new GoogleCloudStorageBucketPathPart(bucketName, relativePath);
    //}

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
    
    //private async Task<bool> ObjectExists(string bucketName, string fileName, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        await _client.GetObjectAsync(bucketName, fileName, null, cancellationToken).ConfigureAwait(false);
    //        return true;
    //    }
    //    catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
    //    {
    //        return false;
    //    }
    //}

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