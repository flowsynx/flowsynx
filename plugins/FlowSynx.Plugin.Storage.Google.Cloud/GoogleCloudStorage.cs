﻿using EnsureThat;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using FlowSynx.IO;
using Google;
using System.Net;
using System.Text;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Plugin.Storage.Options;
using FlowSynx.Data.Filter;
using FlowSynx.Data.Extensions;
using System.Data;

namespace FlowSynx.Plugin.Storage.Google.Cloud;

public class GoogleCloudStorage :PluginBase
{
    private readonly ILogger<GoogleCloudStorage> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private GoogleCloudStorageSpecifications? _cloudStorageSpecifications;
    private StorageClient _client = null!;

    public GoogleCloudStorage(ILogger<GoogleCloudStorage> logger, IDataFilter dataFilter, 
        ISerializer serializer, IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(serializer, nameof(serializer));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataFilter = dataFilter;
        _serializer = serializer;
        _deserializer = deserializer;
    }

    public override Guid Id => Guid.Parse("d3c52770-f001-4ea3-93b7-f113a956a091");
    public override string Name => "Google.Cloud";
    public override PluginNamespace Namespace => PluginNamespace.Storage;
    public override string? Description => Resources.PluginDescription;
    public override PluginSpecifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(GoogleCloudStorageSpecifications);

    public override Task Initialize()
    {
        _cloudStorageSpecifications = Specifications.ToObject<GoogleCloudStorageSpecifications>();
        _client = CreateClient(_cloudStorageSpecifications);
        return Task.CompletedTask;
    }

    public override Task<object> About(PluginBase? inferiorPlugin, 
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new StorageException(Resources.AboutOperrationNotSupported);
    }

    public override async Task CreateAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var createOptions = options.ToObject<CreateOptions>();
        await CreateEntityAsync(entity, createOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task WriteAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var writeOptions = options.ToObject<WriteOptions>();
        await WriteEntityAsync(entity, writeOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(string entity, PluginBase? inferiorPlugin, 
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var readOptions = options.ToObject<ReadOptions>();
        return await ReadEntityAsync(entity, readOptions, cancellationToken).ConfigureAwait(false);
    }

    public override Task UpdateAsync(string entity, PluginBase? inferiorPlugin, 
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listptions = options.ToObject<ListOptions>();
        var deleteOptions = options.ToObject<DeleteOptions>();
        var dataTable = await FilteredEntitiesAsync(path, listptions, cancellationToken).ConfigureAwait(false);
        var entities = dataTable.CreateListFromTable();

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));
        
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageEntity storageEntity)
                continue;

            await DeleteEntityAsync(storageEntity.FullPath, cancellationToken).ConfigureAwait(false);
        }

        if (deleteOptions.Purge is true)
        {
            var pathParts = GetPartsAsync(path);
            await DeleteAllAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken).ConfigureAwait(false);
        }
    }

    public override async Task<bool> ExistAsync(string entity, PluginBase? inferiorPlugin, 
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetPartsAsync(path);

            if (PathHelper.IsFile(path))
                return await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
            
            if (PathHelper.IsRootPath(pathParts.RelativePath))
                return await BucketExists(pathParts.BucketName, cancellationToken);
            
            return await FolderExistAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    public override async Task<IEnumerable<object>> ListAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var listOptions = options.ToObject<ListOptions>();
        var filteredData = await FilteredEntitiesAsync(entity, listOptions, cancellationToken);
        return filteredData.CreateListFromTable();
    }

    public override async Task<TransferData> PrepareTransferring(string entity, PluginBase? inferiorPlugin, 
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var readOptions = options.ToObject<ReadOptions>();

        var storageEntities = await EntitiesAsync(path, listOptions, cancellationToken);

        var fields = DeserializeToStringArray(listOptions.Fields);
        var kindFieldExist = fields.Length == 0 || fields.Any(s => s.Equals("Kind", StringComparison.OrdinalIgnoreCase));
        var fullPathFieldExist = fields.Length == 0 || fields.Any(s => s.Equals("FullPath", StringComparison.OrdinalIgnoreCase));

        if (!kindFieldExist)
            fields = fields.Append("Kind").ToArray();

        if (!fullPathFieldExist)
            fields = fields.Append("FullPath").ToArray();

        var dataFilterOptions = GetDataFilterOptions(listOptions);

        var dataTable = storageEntities.ToDataTable();
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var transferDataRows = new List<TransferDataRow>();

        foreach (DataRow row in filteredData.Rows)
        {
            var content = string.Empty;
            var contentType = string.Empty;
            var fullPath = row["FullPath"].ToString() ?? string.Empty;

            if (string.Equals(row["Kind"].ToString(), StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(fullPath))
                {
                    var read = await ReadEntityAsync(entity, readOptions, cancellationToken).ConfigureAwait(false);
                    content = read.Content.ToBase64String();
                }
            }

            if (!kindFieldExist)
                row["Kind"] = DBNull.Value;

            if (!fullPathFieldExist)
                row["FullPath"] = DBNull.Value;

            var itemArray = row.ItemArray.Where(x => x != DBNull.Value).ToArray();
            transferDataRows.Add(new TransferDataRow
            {
                Key = fullPath,
                ContentType = contentType,
                Content = content,
                Items = itemArray
            });
        }

        if (!kindFieldExist)
            filteredData.Columns.Remove("Kind");

        if (!fullPathFieldExist)
            filteredData.Columns.Remove("FullPath");

        var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
        var result = new TransferData
        {
            PluginNamespace = Namespace,
            PluginType = Type,
            Kind = TransferKind.Copy,
            Columns = columnNames,
            Rows = transferDataRows
        };

        return result;
    }

    public override async Task TransferAsync(string entity, PluginBase? inferiorPlugin, 
        PluginOptions? options, TransferData transferData, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (transferData.PluginNamespace == PluginNamespace.Storage)
        {
            var createOptions = options.ToObject<CreateOptions>();
            var writeOptions = options.ToObject<WriteOptions>();

            foreach (var item in transferData.Rows)
            {
                switch (item.Content)
                {
                    case null:
                    case "":
                        await CreateEntityAsync(item.Key, createOptions, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                        break;
                    case var data:
                        var parentPath = PathHelper.GetParent(item.Key);
                        if (!PathHelper.IsRootPath(parentPath))
                        {
                            await CreateEntityAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
                            await WriteEntityAsync(item.Key, writeOptions, data, cancellationToken).ConfigureAwait(false);
                            _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                        }

                        break;
                }
            }
        }
        else
        {
            var path = PathHelper.ToUnixPath(entity);
            if (!string.IsNullOrEmpty(transferData.Content))
            {
                var fileBytes = Convert.FromBase64String(transferData.Content);
                await File.WriteAllBytesAsync(path, fileBytes, cancellationToken);
            }
            else
            {
                foreach (var item in transferData.Rows)
                {
                    if (item.Content != null)
                    {
                        var parentPath = PathHelper.GetParent(path);
                        var fileBytes = Convert.FromBase64String(item.Content);
                        await File.WriteAllBytesAsync(PathHelper.Combine(parentPath, item.Key), fileBytes, cancellationToken);
                    }
                }
            }
        }
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginBase? inferiorPlugin,
        PluginOptions? options, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var storageEntities = await EntitiesAsync(path, listOptions, cancellationToken);

        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();
        foreach (var entityItem in storageEntities)
        {
            if (!string.Equals(entityItem.Kind, StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"The item '{entityItem.Name}' is not a file.");
                continue;
            }

            try
            {
                var readOptions = new ReadOptions { Hashing = false };
                var content = await ReadEntityAsync(entityItem.FullPath, readOptions, cancellationToken).ConfigureAwait(false);
                compressEntries.Add(new CompressEntry
                {
                    Name = entityItem.Name,
                    ContentType = entityItem.ContentType,
                    Content = content.Content,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                continue;
            }
        }

        return compressEntries;
    }

    #region private methods
    private StorageClient CreateClient(GoogleCloudStorageSpecifications specifications)
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
            universe_domain = specifications.UniverseDomain
        };

        var json = _serializer.Serialize(jsonObject);
        var credential = GoogleCredential.FromJson(json);
        return StorageClient.Create(credential);
    }

    private GoogleCloudStorageBucketPathPart GetPartsAsync(string fullPath)
    {
        fullPath = PathHelper.Normalize(fullPath);
        if (fullPath == null)
            throw new ArgumentNullException(nameof(fullPath));

        string bucketName, relativePath;
        string[] parts = PathHelper.Split(fullPath);

        if (parts.Length == 1)
        {
            bucketName = parts[0];
            relativePath = string.Empty;
        }
        else
        {
            bucketName = parts[0];
            relativePath = PathHelper.Combine(parts.Skip(1));
        }

        return new GoogleCloudStorageBucketPathPart(bucketName, relativePath);
    }

    private async Task ListAsync(string bucketName, List<StorageEntity> result, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        using var browser = new GoogleBucketBrowser(_logger, _client, bucketName);
        IReadOnlyCollection<StorageEntity> objects =
            await browser.ListFolderAsync(path, listOptions, cancellationToken
        ).ConfigureAwait(false);

        if (objects.Count > 0)
        {
            result.AddRange(objects);
        }
    }

    private async Task<IReadOnlyCollection<string>> ListBucketsAsync(CancellationToken cancellationToken)
    {
        if (_cloudStorageSpecifications == null)
            throw new StorageException(Resources.SpecificationsCouldNotBeNullOrEmpty);

        var result = new List<string>();

        await foreach (var bucket in _client.ListBucketsAsync(_cloudStorageSpecifications.ProjectId).ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(bucket.Name))
                result.Add(bucket.Name);
        }

        return result;
    }

    private async Task<bool> BucketExists(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            await _client.GetBucketAsync(bucketName, null, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task<bool> ObjectExists(string bucketName, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            await _client.GetObjectAsync(bucketName, fileName, null, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task<bool> FolderExistAsync(string bucketName, string path, CancellationToken cancellationToken)
    {
        var folderPrefix = path + PathHelper.PathSeparator;
        var request = _client.Service.Objects.List(bucketName);
        request.Prefix = folderPrefix;
        request.Delimiter = PathHelper.PathSeparatorString;

        var serviceObjects = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (serviceObjects == null)
            return false;

        if (serviceObjects.Items is { Count: > 0 })
            return serviceObjects.Items?.Any(x => x.Name.StartsWith(folderPrefix)) ?? false;
        
        return serviceObjects.Prefixes?.Any(x => x.StartsWith(folderPrefix)) ?? false;
    }

    private async Task AddFolder(string bucketName, string folderName, CancellationToken cancellationToken)
    {
        if (!folderName.EndsWith(PathHelper.PathSeparator))
            folderName += PathHelper.PathSeparator;

        var content = Encoding.UTF8.GetBytes("");
        await _client.UploadObjectAsync(bucketName, folderName, "application/x-directory", 
            new MemoryStream(content), cancellationToken: cancellationToken);
        _logger.LogInformation($"Folder '{folderName}' was created successfully.");
    }

    private async Task<bool> DeleteEntityAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetPartsAsync(path);
            if (PathHelper.IsFile(path))
            {
                var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
                if (!isExist)
                    return false;

                await _client.DeleteObjectAsync(pathParts.BucketName, pathParts.RelativePath,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return true;
            }
            
            await DeleteAllAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken: cancellationToken);
            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task DeleteAllAsync(string bucketName, string folderName, CancellationToken cancellationToken)
    {
        var request = _client.Service.Objects.List(bucketName);
        request.Prefix = folderName;
        request.Delimiter = null;

        do
        {
            var serviceObjects = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (serviceObjects.Items != null)
            {
                foreach (var item in serviceObjects.Items)
                {
                    if (item == null)
                        continue;

                    await _client.DeleteObjectAsync(bucketName, item.Name, 
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            request.PageToken = serviceObjects.NextPageToken;
        }
        while (request.PageToken != null);
    }

    private async Task CreateEntityAsync(string entity, CreateOptions options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        if (_cloudStorageSpecifications == null)
            throw new StorageException(Resources.SpecificationsCouldNotBeNullOrEmpty);

        var pathParts = GetPartsAsync(path);
        var isExist = await BucketExists(pathParts.BucketName, cancellationToken);
        if (!isExist)
        {
            await _client.CreateBucketAsync(_cloudStorageSpecifications.ProjectId, pathParts.BucketName,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            _logger.LogInformation($"Bucket '{pathParts.BucketName}' was created successfully.");
        }

        if (!string.IsNullOrEmpty(pathParts.RelativePath))
        {
            await AddFolder(pathParts.BucketName, pathParts.RelativePath, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteEntityAsync(string entity, WriteOptions options, 
        object dataOptions, CancellationToken cancellationToken = new CancellationToken())
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
            var pathParts = GetPartsAsync(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);

            if (isExist && options.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            await _client.UploadObjectAsync(pathParts.BucketName, pathParts.RelativePath,
                null, dataStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task<ReadResult> ReadEntityAsync(string entity, ReadOptions options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            var pathParts = GetPartsAsync(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);

            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var ms = new MemoryStream();
            var response = await _client.DownloadObjectAsync(pathParts.BucketName,
                pathParts.RelativePath, ms, cancellationToken: cancellationToken).ConfigureAwait(false);

            ms.Position = 0;

            return new ReadResult
            {
                Content = ms.ToArray(),
                ContentHash = Convert.FromBase64String(response.Md5Hash).ToHexString(),
            };
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task<DataTable> FilteredEntitiesAsync(string entity, ListOptions options,
    CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);
        var storageEntities = await EntitiesAsync(path, options, cancellationToken);

        var dataFilterOptions = GetDataFilterOptions(options);
        var dataTable = storageEntities.ToDataTable();
        var result = _dataFilter.Filter(dataTable, dataFilterOptions);

        return result;
    }

    private async Task<IEnumerable<StorageEntity>> EntitiesAsync(string entity, ListOptions options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        var buckets = new List<string>();

        var dataFilterOptions = GetDataFilterOptions(options);

        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
        {
            buckets.AddRange(await ListBucketsAsync(cancellationToken).ConfigureAwait(false));
            storageEntities.AddRange(buckets.Select(b => b.ToEntity(options.IncludeMetadata)));

            if (!options.Recurse)
            {
                return storageEntities;
            }
        }
        else
        {
            var pathParts = GetPartsAsync(path);
            path = pathParts.RelativePath;
            buckets.Add(pathParts.BucketName);
        }

        await Task.WhenAll(buckets.Select(b =>
            ListAsync(b, storageEntities, path, options, cancellationToken))
        ).ConfigureAwait(false);

        return storageEntities;
    }

    private DataFilterOptions GetDataFilterOptions(ListOptions options)
    {
        var fields = DeserializeToStringArray(options.Fields);
        var dataFilterOptions = new DataFilterOptions
        {
            Fields = fields,
            FilterExpression = options.Filter,
            SortExpression = options.Sort,
            CaseSensitive = options.CaseSensitive,
            Limit = options.Limit,
        };

        return dataFilterOptions;
    }

    private string[] DeserializeToStringArray(string? fields)
    {
        var result = Array.Empty<string>();
        if (!string.IsNullOrEmpty(fields))
        {
            result = _deserializer.Deserialize<string[]>(fields);
        }

        return result;
    }

    #endregion
}