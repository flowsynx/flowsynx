using Azure.Storage.Blobs;
using FlowSynx.Connectors.Abstractions;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Filter;
using FlowSynx.Data.Extensions;
using System.Data;
using FlowSynx.Connectors.Storage.Exceptions;
using FlowSynx.Connectors.Storage.Azure.Blobs.Models;
using FlowSynx.Connectors.Storage.Azure.Blobs.Services;

namespace FlowSynx.Connectors.Storage.Azure.Blobs;

public class AzureBlobConnector : Connector
{
    private readonly ILogger<AzureBlobConnector> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private AzureBlobSpecifications? _azureBlobSpecifications;
    private BlobServiceClient _client = null!;
    private readonly IAzureBlobConnection _connection;
    private IAzureBlobManager? _browser;

    public AzureBlobConnector(ILogger<AzureBlobConnector> logger, IDataFilter dataFilter,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
        _connection = new AzureBlobConnection();
    }

    public override Guid Id => Guid.Parse("7f21ba04-ea2a-4c78-a2f9-051fa05391c8");
    public override string Name => "Azure.Blobs";
    public override Namespace Namespace => Namespace.Storage;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AzureBlobSpecifications);

    public override Task Initialize()
    {
        _azureBlobSpecifications = Specifications.ToObject<AzureBlobSpecifications>();
        _client = _connection.GetClient(_azureBlobSpecifications);
        _browser = new AzureBlobManager(_logger, _client);
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
        var browser = GetBrowser();

        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var createOptions = options.ToObject<CreateOptions>();
        await browser.CreateAsync(context.Entity, createOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task WriteAsync(Context context, ConnectorOptions? options, 
        object dataOptions, CancellationToken cancellationToken = default)
    {
        var browser = GetBrowser();

        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var writeOptions = options.ToObject<WriteOptions>();
        await browser.WriteAsync(context.Entity, writeOptions, dataOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        var browser = GetBrowser();

        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var readOptions = options.ToObject<ReadOptions>();
        return await browser.ReadAsync(context.Entity, readOptions, cancellationToken).ConfigureAwait(false);
    }

    public override Task UpdateAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        var browser = GetBrowser();

        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);
        var listOptions = options.ToObject<ListOptions>();
        var deleteOptions = options.ToObject<DeleteOptions>();

        var dataTable = await FilteredEntitiesAsync(path, listOptions, cancellationToken).ConfigureAwait(false);
        var entities = dataTable.CreateListFromTable();

        var storageEntities = entities.ToList();
        if (!storageEntities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));
        
        foreach (var entityItem in storageEntities)
        {
            if (entityItem is not StorageEntity storageEntity)
                continue;

            await browser.DeleteAsync(storageEntity.FullPath, cancellationToken).ConfigureAwait(false);
        }

        if (deleteOptions.Purge is true)
        {
           await browser.PurgeAsync(path, cancellationToken);
        }
    }

    public override async Task<bool> ExistAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        var browser = GetBrowser();

        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        return await browser.ExistAsync(context.Entity, cancellationToken).ConfigureAwait(false);
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
        var browser = GetBrowser();

        var createOptions = options.ToObject<CreateOptions>();
        var writeOptions = options.ToObject<WriteOptions>();

        var path = PathHelper.ToUnixPath(context.Entity);

        if (!string.IsNullOrEmpty(transferData.Content))
        {
            var parentPath = PathHelper.GetParent(path);
            if (!PathHelper.IsRootPath(parentPath))
            {
                await browser.CreateAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
                await browser.WriteAsync(path, writeOptions, transferData.Content, cancellationToken).ConfigureAwait(false);
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
                        await browser.CreateAsync(item.Key, createOptions, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
                else
                {
                    var parentPath = PathHelper.GetParent(item.Key);
                    if (!PathHelper.IsRootPath(parentPath))
                    {
                        await browser.CreateAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
                        await browser.WriteAsync(item.Key, writeOptions, item.Content, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
            }
        }
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context,
        ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        var browser = GetBrowser();

        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);
        var listOptions = options.ToObject<ListOptions>();
        var storageEntities = await browser.ListAsync(path, listOptions, cancellationToken);

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
                var stream = await browser.ReadAsync(entityItem.FullPath, readOptions, cancellationToken);
                compressEntries.Add(new CompressEntry
                {
                    Name = entityItem.Name,
                    ContentType = entityItem.ContentType,
                    Content = stream.Content,
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
    private async Task<DataTable> FilteredEntitiesAsync(string entity, ListOptions options,
        CancellationToken cancellationToken)
    {
        var browser = GetBrowser();

        var path = PathHelper.ToUnixPath(entity);
        var storageEntities = await browser.ListAsync(path, options, cancellationToken);

        var dataFilterOptions = GetDataFilterOptions(options);
        var dataTable = storageEntities.ToDataTable();
        var result = _dataFilter.Filter(dataTable, dataFilterOptions);

        return result;
    }

    private async Task<TransferData> PrepareTransferring(Context context, ListOptions listOptions, 
        ReadOptions readOptions, CancellationToken cancellationToken)
    {
        var browser = GetBrowser();

        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);

        var storageEntities = await browser.ListAsync(path, listOptions, cancellationToken);

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
                    var read = await browser.ReadAsync(fullPath, readOptions, cancellationToken).ConfigureAwait(false);
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
            Namespace = Namespace,
            ConnectorType = Type,
            Kind = TransferKind.Copy,
            Columns = columnNames,
            Rows = transferDataRows
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

    private string[] DeserializeToStringArray(string? fields)
    {
        var result = Array.Empty<string>();
        if (!string.IsNullOrEmpty(fields))
        {
            result = _deserializer.Deserialize<string[]>(fields);
        }

        return result;
    }

    private IAzureBlobManager GetBrowser()
    {
        return _browser ?? new AzureBlobManager(_logger, _client);
    }
    #endregion
}