using Azure;
using Azure.Storage;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Plugin.Abstractions;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Storage.Abstractions.Exceptions;
using FlowSynx.Plugin.Storage.Options;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Filter;
using FlowSynx.Data.Extensions;
using System.Data;

namespace FlowSynx.Plugin.Storage.Azure.Files;

public class AzureFileStorage : PluginBase
{
    private readonly ILogger<AzureFileStorage> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private AzureFilesSpecifications? _azureFilesSpecifications;
    private ShareClient _client = null!;

    public AzureFileStorage(ILogger<AzureFileStorage> logger, IDataFilter dataFilter,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
    }

    public override Guid Id => Guid.Parse("cd7d1271-ce52-4cc3-b0b4-3f4f72b2fa5d");
    public override string Name => "Azure.Files";
    public override PluginNamespace Namespace => PluginNamespace.Storage;
    public override string? Description => Resources.PluginDescription;
    public override PluginSpecifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AzureFilesSpecifications);

    public override Task Initialize()
    {
        _azureFilesSpecifications = Specifications.ToObject<AzureFilesSpecifications>();
        _client = CreateClient(_azureFilesSpecifications);
        return Task.CompletedTask;
    }

    public override async Task<object> About(PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        long totalUsed;
        var aboutOptions = options.ToObject<AboutOptions>();

        try
        {
            var state = await _client.GetStatisticsAsync(cancellationToken);
            totalUsed = state.Value.ShareUsageInBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            totalUsed = 0;
        }

        return new
        {
            Total = totalUsed
        };
    }

    public override async Task CreateAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var createOptions = options.ToObject<CreateOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        try
        {
            var pathParts = PathHelper.Split(path);
            string proceedPath = string.Empty;
            foreach (var part in pathParts)
            {
                proceedPath = PathHelper.Combine(proceedPath, part);
                ShareDirectoryClient directoryClient = _client.GetDirectoryClient(proceedPath);
                await directoryClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new StorageException(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public override async Task WriteAsync(string entity, PluginOptions? options, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var writeOptions = options.ToObject<WriteOptions>();

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
            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);

            var isExist = await fileClient.ExistsAsync(cancellationToken: cancellationToken);
            if (isExist && writeOptions.Overwrite is false)
                throw new StorageException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

            var parentPath = PathHelper.GetParent(path) + PathHelper.PathSeparatorString;
            await CreateAsync(parentPath, null, cancellationToken);

            await fileClient.CreateAsync(maxSize: dataStream.Length, cancellationToken: cancellationToken);
            await fileClient.UploadRangeAsync(new HttpRange(0, dataStream.Length), dataStream, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new StorageException(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidResourceName)
        {
            throw new StorageException(Resources.TheSpecifiedResourceNameContainsInvalidCharacters);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public override async Task<object> ReadAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var readOptions = options.ToObject<ReadOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StorageException(Resources.ThePathIsNotFile);

        try
        {
            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);

            var isExist = await fileClient.ExistsAsync(cancellationToken: cancellationToken);
            if (!isExist)
                throw new StorageException(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            var stream = await fileClient.OpenReadAsync(cancellationToken: cancellationToken);
            var fileProperties = await fileClient.GetPropertiesAsync(cancellationToken);
            var fileExtension = Path.GetExtension(path);

            return new StorageRead()
            {
                Stream = new StorageStream(stream),
                ContentType = fileProperties.Value.ContentType,
                Extension = fileExtension,
                Md5 = fileProperties.Value.ContentHash?.ToHexString(),
            };
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new StorageException(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public override Task UpdateAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var deleteOptions = options.ToObject<DeleteOptions>();
        var entities = await ListAsync(path, options, cancellationToken).ConfigureAwait(false);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));
        
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageList list)
                continue;

            await DeleteEntityAsync(list.Path, cancellationToken).ConfigureAwait(false);
        }

        if (deleteOptions.Purge is true)
        {
            ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
            await directoryClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }

    public override async Task<bool> ExistAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        if (string.IsNullOrWhiteSpace(path))
            throw new StorageException(Resources.ThePathMustBeFile);

        try
        {
            if (PathHelper.IsDirectory(path))
            {
                ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
                return await directoryClient.ExistsAsync(cancellationToken: cancellationToken);
            }

            ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);
            return await fileClient.ExistsAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new StorageException(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    public override async Task<IEnumerable<object>> ListAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);

        if (string.IsNullOrEmpty(path))
            path += PathHelper.PathSeparator;

        if (!PathHelper.IsDirectory(path))
            throw new StorageException(Resources.ThePathIsNotDirectory);

        var storageEntities = new List<StorageEntity>();
        ShareDirectoryClient directoryClient;
        var listOptions = options.ToObject<ListOptions>();

        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
            directoryClient = _client.GetRootDirectoryClient();
        else
            directoryClient = _client.GetDirectoryClient(path);

        var remaining = new Queue<ShareDirectoryClient>();
        remaining.Enqueue(directoryClient);
        while (remaining.Count > 0)
        {
            ShareDirectoryClient dir = remaining.Dequeue();
            try
            {
                await foreach (ShareFileItem item in dir.GetFilesAndDirectoriesAsync(cancellationToken: cancellationToken))
                {
                    try
                    {
                        if (item.IsDirectory)
                            storageEntities.Add(await dir.ToEntity(item, listOptions.IncludeMetadata,
                                cancellationToken));
                        else
                            storageEntities.Add(await dir.ToEntity(item, dir.GetFileClient(item.Name),
                                listOptions.IncludeMetadata, cancellationToken));

                        if (!listOptions.Recurse) continue;

                        if (item.IsDirectory)
                        {
                            remaining.Enqueue(dir.GetSubdirectoryClient(item.Name));
                        }
                    }
                    catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
                    {
                        _logger.LogError(string.Format(Resources.ShareItemNotFound, item.Name));
                    }
                }
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
            {
                throw new StorageException(string.Format(Resources.ResourceNotExist, dir.Name));
            }
        }

        var fields = DeserializeToStringArray(listOptions.Fields);
        var dataFilterOptions = new DataFilterOptions
        {
            Fields = fields,
            FilterExpression = listOptions.Filter,
            SortExpression = listOptions.Sort,
            CaseSensitive = listOptions.CaseSensitive,
            Limit = listOptions.Limit,
        };

        var dataTable = storageEntities.ToDataTable();
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        return filteredData.CreateListFromTable();
    }

    public override async Task<TransmissionData> PrepareTransmissionData(string entity, PluginOptions? options,
            CancellationToken cancellationToken = new CancellationToken())
    {
        return await PrepareCopyDirectory(entity, options, cancellationToken);
    }

    //private async Task<TransmissionData> PrepareCopyFile(string entity, CancellationToken cancellationToken = default)
    //{
    //    var sourceStream = await ReadAsync(entity, null, cancellationToken);

    //    if (sourceStream is not StorageRead storageRead)
    //        throw new StorageException(string.Format(Resources.CopyOperationCouldNotBeProceed, entity));

    //    return new TransmissionData(entity, storageRead.Stream, storageRead.ContentType);
    //}

    private Task<TransmissionData> PrepareCopyDirectory(string entity, PluginOptions? options,
        CancellationToken cancellationToken = default)
    {
        //var entities = await ListAsync(entity, options, cancellationToken).ConfigureAwait(false);
        //var storageEntities = entities.ToList().ConvertAll(item => (StorageList)item);

        //var result = new List<TransmissionData>(storageEntities.Count);

        //foreach (var entityItem in storageEntities)
        //{
        //    TransmissionData transmissionData;
        //    if (string.Equals(entityItem.Kind, StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
        //    {
        //        var read = await ReadAsync(entityItem.Path, null, cancellationToken);
        //        if (read is not StorageRead storageRead)
        //        {
        //            _logger.LogWarning($"The item '{entityItem.Name}' could be not read.");
        //            continue;
        //        }
        //        transmissionData = new TransmissionData(entityItem.Path, storageRead.Stream, storageRead.ContentType);
        //    }
        //    else
        //    {
        //        transmissionData = new TransmissionData(entityItem.Path);
        //    }

        //    result.Add(transmissionData);
        //}

        //return result;
        var dataTable = new System.Data.DataTable();
        var result = new TransmissionData
        {
            PluginNamespace = this.Namespace,
            PluginType = this.Type,
            Columns = dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName),
            Rows = new List<TransmissionDataRow>()
            {
                new TransmissionDataRow {
                    Key = Guid.NewGuid().ToString(),
                    Content = string.Empty,
                    Items = dataTable.Rows.Cast<DataRow>().First().ItemArray,
                    ContentType = ""
                }
            }
        };

        return Task.FromResult(result);
    }

    public override async Task TransmitDataAsync(string entity, PluginOptions? options, 
        TransmissionData transmissionData, CancellationToken cancellationToken = new CancellationToken())
    {
        var result = new List<object>();
        //var data = transmissionData.ToList();
        //foreach (var item in data)
        //{
        //    switch (item.Content)
        //    {
        //        case null:
        //            result.Add(await CreateAsync(item.Key, options, cancellationToken));
        //            _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
        //            break;
        //        case StorageStream stream:
        //            var parentPath = PathHelper.GetParent(item.Key);
        //            if (!PathHelper.IsRootPath(parentPath))
        //            {
        //                await CreateAsync(parentPath, options, cancellationToken);
        //                result.Add(await WriteAsync(item.Key, options, stream, cancellationToken));
        //                _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
        //            }
        //            break;
        //    }
        //}
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var entities = await ListAsync(path, options, cancellationToken).ConfigureAwait(false);

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));

        var compressEntries = new List<CompressEntry>();
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageList entry)
            {
                _logger.LogWarning("The item is not valid object type. It should be StorageEntity type.");
                continue;
            }

            if (!string.Equals(entry.Kind, StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"The item '{entry.Name}' is not a file.");
                continue;
            }

            try
            {
                var stream = await ReadAsync(entry.Path, options, cancellationToken);
                if (stream is not StorageRead storageRead)
                {
                    _logger.LogWarning($"The item '{entry.Name}' could be not read.");
                    continue;
                }

                compressEntries.Add(new CompressEntry
                {
                    Name = entry.Name,
                    ContentType = entry.ContentType,
                    Stream = storageRead.Stream,
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

    public void Dispose() { }

    #region internal methods
    private ShareClient CreateClient(AzureFilesSpecifications specifications)
    {
        if (string.IsNullOrEmpty(specifications.ShareName))
            throw new StorageException(Resources.ShareNameInSpecificationShouldBeNotEmpty);

        if (!string.IsNullOrEmpty(specifications.ConnectionString))
            return new ShareClient(specifications.ConnectionString, specifications.ShareName);

        if (string.IsNullOrEmpty(specifications.AccountKey) || string.IsNullOrEmpty(specifications.AccountName))
            throw new StorageException(Resources.OnePropertyShouldHaveValue);

        var uri = new Uri($"https://{specifications.AccountName}.file.core.windows.net/{specifications.ShareName}");
        var credential = new StorageSharedKeyCredential(specifications.AccountName, specifications.AccountKey);
        return new ShareClient(shareUri: uri, credential: credential);
    }

    private async Task<bool> DeleteEntityAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
            throw new StorageException(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            if (PathHelper.IsFile(path))
            {
                ShareFileClient fileClient = _client.GetRootDirectoryClient().GetFileClient(path);
                return await fileClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }

            ShareDirectoryClient directoryClient = _client.GetDirectoryClient(path);
            await DeleteAllAsync(directoryClient, cancellationToken: cancellationToken);
            return true;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ResourceNotFound)
        {
            throw new StorageException(string.Format(Resources.ResourceNotExist, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ShareNotFound)
        {
            throw new StorageException(string.Format(Resources.ShareItemNotFound, path));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.InvalidUri)
        {
            throw new StorageException(Resources.InvalidPathEntered);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == ShareErrorCode.ParentNotFound)
        {
            throw new StorageException(Resources.ParentNotFound);
        }
        catch (RequestFailedException)
        {
            throw new StorageException(Resources.SomethingWrongHappenedDuringProcessingExistingFile);
        }
    }

    private async Task DeleteAllAsync(ShareDirectoryClient dirClient, CancellationToken cancellationToken)
    {

        await foreach (ShareFileItem item in dirClient.GetFilesAndDirectoriesAsync())
        {
            if (item.IsDirectory)
            {
                var subDir = dirClient.GetSubdirectoryClient(item.Name);
                await DeleteAllAsync(subDir, cancellationToken: cancellationToken);
            }
            else
            {
                await dirClient.DeleteFileAsync(item.Name, cancellationToken: cancellationToken);
            }
        }

        await dirClient.DeleteAsync(cancellationToken);
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