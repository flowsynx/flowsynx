﻿using System.Net;
using Amazon.S3;
using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using Microsoft.Extensions.Logging;
using Amazon;
using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Filter;
using FlowSynx.Data.Extensions;
using System.Data;
using FlowSynx.Connectors.Storage.Exceptions;

namespace FlowSynx.Connectors.Storage.Amazon.S3;

public class AmazonS3Connector : Connector
{
    private readonly ILogger<AmazonS3Connector> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private AmazonS3StorageSpecifications? _s3StorageSpecifications;
    private AmazonS3Client _client = null!;
    private TransferUtility _fileTransferUtility = null!;

    public AmazonS3Connector(ILogger<AmazonS3Connector> logger, IDataFilter dataFilter,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
    }

    public override Guid Id => Guid.Parse("b961131b-04cb-48df-9554-4252dc66c04c");
    public override string Name => "Amazon.S3";
    public override Namespace Namespace => Namespace.Storage;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AmazonS3StorageSpecifications);

    public override Task Initialize()
    {
        _s3StorageSpecifications = Specifications.ToObject<AmazonS3StorageSpecifications>();
        _client = CreateClient(_s3StorageSpecifications);
        _fileTransferUtility = CreateTransferUtility(_client);
        return Task.CompletedTask;
    }

    public override Task<object> About(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        throw new StorageException(Resources.AboutOperrationNotSupported);
    }

    public override async Task CreateAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var createOptions = options.ToObject<CreateOptions>();
        await CreateEntityAsync(context.Entity, createOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task WriteAsync(Context context, ConnectorOptions? options, 
        object dataOptions, CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var writeFilters = options.ToObject<WriteOptions>();
        await WriteEntityAsync(context.Entity, writeFilters, dataOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var readOptions = options.ToObject<ReadOptions>();
        return await ReadEntityAsync(context.Entity, readOptions, cancellationToken).ConfigureAwait(false);
    }

    public override Task UpdateAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);
        var listOptions = options.ToObject<ListOptions>();
        var deleteFilters = options.ToObject<DeleteOptions>();

        var dataTable = await FilteredEntitiesAsync(path, listOptions, cancellationToken).ConfigureAwait(false);
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

        if (deleteFilters.Purge is true)
        {
            var pathParts = GetPartsAsync(path);
            var folder = pathParts.RelativePath;
            if (!folder.EndsWith(PathHelper.PathSeparator))
                folder += PathHelper.PathSeparator;

            await _client.DeleteObjectAsync(pathParts.BucketName, folder, cancellationToken);
        }
    }

    public override async Task<bool> ExistAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        return await ExistEntityAsync(context.Entity, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var listOptions = options.ToObject<ListOptions>();
        var filteredData = await FilteredEntitiesAsync(context.Entity, listOptions, cancellationToken);
        return filteredData.CreateListFromTable();
    }

    public override async Task TransferAsync(Context sourceContext, Connector destinationConnector,
        Context destinationContext, ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        if (destinationConnector is null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var listOptions = options.ToObject<ListOptions>();
        var readOptions = options.ToObject<ReadOptions>();

        var transferData = await PrepareTransferring(sourceContext, listOptions, readOptions, cancellationToken);

        foreach (var row in transferData.Rows)
            row.Key = row.Key.Replace(sourceContext.Entity, destinationContext.Entity);
        
        await destinationConnector.ProcessTransferAsync(destinationContext, transferData, options, cancellationToken);
    }

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        var createOptions = options.ToObject<CreateOptions>();
        var writeOptions = options.ToObject<WriteOptions>();

        var path = PathHelper.ToUnixPath(context.Entity);

        if (!string.IsNullOrEmpty(transferData.Content))
        {
            var parentPath = PathHelper.GetParent(path);
            if (!PathHelper.IsRootPath(parentPath))
            {
                await CreateEntityAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
                await WriteEntityAsync(path, writeOptions, transferData.Content, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation($"Copy operation done for entity '{path}'");
            }
        }
        else
        {
            foreach (var item in transferData.Rows)
            {
                if (string.IsNullOrEmpty(item.Content))
                {
                    if (transferData.Namespace == Namespace.Storage)
                    {
                        await CreateEntityAsync(item.Key, createOptions, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
                else
                {
                    var parentPath = PathHelper.GetParent(item.Key);
                    if (!PathHelper.IsRootPath(parentPath))
                    {
                        await CreateEntityAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
                        await WriteEntityAsync(item.Key, writeOptions, item.Content, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
            }
        }
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context,
        ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);
        var listOptions = options.ToObject<ListOptions>();

        var storageEntities = await EntitiesAsync(path, listOptions, cancellationToken);

        var entityItems = storageEntities.ToList();
        if (!entityItems.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();
        foreach (var entityItem in entityItems)
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
            }
        }

        return compressEntries;
    }

    #region private methods
    private AmazonS3Client CreateClient(AmazonS3StorageSpecifications specifications)
    {
        if (specifications.AccessKey == null)
            throw new ArgumentNullException(nameof(specifications.AccessKey));

        if (specifications.SecretKey == null)
            throw new ArgumentNullException(nameof(specifications.SecretKey));

        var awsCredentials = (string.IsNullOrEmpty(specifications.SessionToken))
            ? (AWSCredentials)new BasicAWSCredentials(specifications.AccessKey, specifications.SecretKey)
            : new SessionAWSCredentials(specifications.AccessKey, specifications.SecretKey, specifications.SessionToken);

        var config = CreateConfig(specifications.Region);
        return new AmazonS3Client(awsCredentials, config);
    }

    private TransferUtility CreateTransferUtility(AmazonS3Client client)
    {
        return new TransferUtility(client, new TransferUtilityConfig());
    }

    private AmazonS3StorageBucketPathPart GetPartsAsync(string fullPath)
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

        return new AmazonS3StorageBucketPathPart(bucketName, relativePath);
    }

    private async Task ListAsync(string bucketName, List<StorageEntity> result, string path,
        ListOptions listOptions, CancellationToken cancellationToken)
    {
        using var browser = new AmazonS3BucketBrowser(_logger, _client, bucketName);
        IReadOnlyCollection<StorageEntity> objects =
            await browser.ListAsync(path, listOptions, cancellationToken).ConfigureAwait(false);

        if (objects.Count > 0)
        {
            result.AddRange(objects);
        }
    }

    private async Task<IReadOnlyCollection<string>> ListBucketsAsync(CancellationToken cancellationToken)
    {
        var buckets = await _client.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
        return buckets.Buckets
            .Where(bucket => !string.IsNullOrEmpty(bucket.BucketName))
            .Select(bucket => bucket.BucketName).ToList();
    }

    private async Task<bool> ObjectExists(string bucketName, string key, CancellationToken cancellationToken)
    {
        try
        {
            var request = new ListObjectsRequest { BucketName = bucketName, Prefix = key };
            var response = await _client.ListObjectsAsync(request, cancellationToken).ConfigureAwait(false);
            return response is { S3Objects.Count: > 0 };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task<bool> BucketExists(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            var bucketsResponse = await _client.ListBucketsAsync(cancellationToken);
            return (bucketsResponse.Buckets.Any(x => x.BucketName == bucketName));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task AddFolder(string bucketName, string folderName, CancellationToken cancellationToken)
    {
        if (!folderName.EndsWith(PathHelper.PathSeparator))
            folderName += PathHelper.PathSeparator;

        var request = new PutObjectRequest()
        {
            BucketName = bucketName,
            StorageClass = S3StorageClass.Standard,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.None,
            Key = folderName,
            ContentBody = string.Empty
        };
        await _client.PutObjectAsync(request, cancellationToken);
        _logger.LogInformation($"Folder '{folderName}' was created successfully.");
    }

    private async Task DeleteEntityAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetPartsAsync(path);
            var isExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
            if (!isExist)
            {
                _logger.LogWarning(string.Format(Resources.TheSpecifiedPathIsNotExist, path));
                return;
            }

            await _client
                .DeleteObjectAsync(pathParts.BucketName, pathParts.RelativePath, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private AmazonS3Config CreateConfig(string? region)
    {
        if (region == null)
            return new AmazonS3Config();

        return new AmazonS3Config
        {
            RegionEndpoint = ToRegionEndpoint(region),
        };
    }

    private RegionEndpoint ToRegionEndpoint(string? region)
    {
        if (region is null)
            throw new ArgumentNullException(nameof(region));

        return RegionEndpoint.GetBySystemName(region);
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

    private async Task<DataTable> FilteredEntitiesAsync(string entity, ListOptions options,
    CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        var storageEntities = await EntitiesAsync(path, options, cancellationToken);

        var dataFilterOptions = GetDataFilterOptions(options);
        var dataTable = storageEntities.ToDataTable();
        var result = _dataFilter.Filter(dataTable, dataFilterOptions);

        return result;
    }

    private async Task<IEnumerable<StorageEntity>> EntitiesAsync(string entity, ListOptions options,
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        var buckets = new List<string>();

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

    private async Task CreateEntityAsync(string entity, CreateOptions options, 
        CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var pathParts = GetPartsAsync(path);
        var isExist = await BucketExists(pathParts.BucketName, cancellationToken);
        if (!isExist)
        {
            await _client.PutBucketAsync(pathParts.BucketName, cancellationToken: cancellationToken).ConfigureAwait(false);
            _logger.LogInformation($"Bucket '{pathParts.BucketName}' was created successfully.");
        }

        if (!string.IsNullOrEmpty(pathParts.RelativePath))
        {
            var isFolderExist = await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
            if (!isFolderExist)
                await AddFolder(pathParts.BucketName, pathParts.RelativePath, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteEntityAsync(string entity, WriteOptions options, 
        object dataOptions, CancellationToken cancellationToken)
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

            await _fileTransferUtility.UploadAsync(dataStream, pathParts.BucketName,
                pathParts.RelativePath, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task<ReadResult> ReadEntityAsync(string entity, ReadOptions options, 
        CancellationToken cancellationToken)
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

            var request = new GetObjectRequest { BucketName = pathParts.BucketName, Key = pathParts.RelativePath };
            var response = await _client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);

            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, cancellationToken);
            ms.Seek(0, SeekOrigin.Begin);

            return new ReadResult
            {
                Content = ms.ToArray(),
                ContentHash = options.Hashing is true ? response.ETag.Trim('\"') : string.Empty,
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task<bool> ExistEntityAsync(string entity, CancellationToken cancellationToken)
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            var pathParts = GetPartsAsync(path);
            return await ObjectExists(pathParts.BucketName, pathParts.RelativePath, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task<TransferData> PrepareTransferring(Context context, ListOptions listOptions,
        ReadOptions readOptions, CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);

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
        var transferDataRow = new List<TransferDataRow>();

        foreach (DataRow row in filteredData.Rows)
        {
            var content = string.Empty;
            var contentType = string.Empty;
            var fullPath = row["FullPath"].ToString() ?? string.Empty;

            if (string.Equals(row["Kind"].ToString(), StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(fullPath))
                {
                    var read = await ReadEntityAsync(context.Entity, readOptions, cancellationToken).ConfigureAwait(false);
                    content = read.Content.ToBase64String();
                }
            }

            if (!kindFieldExist)
                row["Kind"] = DBNull.Value;

            if (!fullPathFieldExist)
                row["FullPath"] = DBNull.Value;

            var itemArray = row.ItemArray.Where(x => x != DBNull.Value).ToArray();
            transferDataRow.Add(new TransferDataRow
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
            Namespace = Namespace,
            ConnectorType = Type,
            Kind = TransferKind.Copy,
            Columns = columnNames,
            Rows = transferDataRow
        };

        return result;
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
    #endregion
}