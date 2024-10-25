using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Filter;
using FlowSynx.Data.Extensions;
using FlowSynx.Connectors.Storage.Exceptions;
using FlowSynx.Connectors.Storage.LocalFileSystem.Models;
using FlowSynx.Connectors.Storage.LocalFileSystem.Services;

namespace FlowSynx.Connectors.Storage.LocalFileSystem;

public class LocalFileSystemConnector : Connector
{
    private readonly ILogger<LocalFileSystemConnector> _logger;
    private readonly IDataFilter _dataFilter;
    private readonly IDeserializer _deserializer;
    private ILocalFileManager _manager = null!;

    public LocalFileSystemConnector(ILogger<LocalFileSystemConnector> logger, IDataFilter dataFilter,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
    }

    public override Guid Id => Guid.Parse("f6304870-0294-453e-9598-a82167ace653");
    public override string Name => "LocalFileSystem";
    public override Namespace Namespace => Namespace.Storage;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(LocalFileSystemSpecifications);

    public override Task Initialize()
    {
        _manager = new LocalFileManager(_logger, _dataFilter, _deserializer);
        return Task.CompletedTask;
    }

    public override async Task<object> About(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);
        
        return await _manager.GetStatisticsAsync();
    }

    public override async Task CreateAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var createOptions = options.ToObject<CreateOptions>();
        await _manager.CreateAsync(context.Entity, createOptions).ConfigureAwait(false);
    }

    public override async Task WriteAsync(Context context, ConnectorOptions? options, 
        object dataOptions, CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var writeOptions = options.ToObject<WriteOptions>();
        await _manager.WriteAsync(context.Entity, writeOptions, dataOptions).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var readOptions = options.ToObject<ReadOptions>();
        return await _manager.ReadAsync(context.Entity, readOptions).ConfigureAwait(false);
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
        var deleteOptions = options.ToObject<DeleteOptions>();

        var entities = await _manager.FilteredEntitiesAsync(path, listOptions).ConfigureAwait(false);

        if (!entities.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));
        
        foreach (var entityItem in entities)
        {
            if (entityItem is not StorageEntity storageEntity) 
                continue;

            await _manager.DeleteAsync(storageEntity.FullPath);
        }

        if (deleteOptions.Purge is true)
            await _manager.PurgeAsync(path);
    }

    public override Task<bool> ExistAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        return _manager.ExistAsync(context.Entity);
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var listOptions = options.ToObject<ListOptions>();
        return await _manager.FilteredEntitiesAsync(context.Entity, listOptions);
    }

    public override async Task TransferAsync(Context sourceContext, Connector destinationConnector,
        Context destinationContext, ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        if (destinationConnector is null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var listOptions = options.ToObject<ListOptions>();
        var readOptions = options.ToObject<ReadOptions>();

        var transferData = await _manager.PrepareDataForTransferring(Namespace, Type, sourceContext.Entity, 
            listOptions, readOptions);

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
                await _manager.CreateAsync(parentPath, createOptions).ConfigureAwait(false);
                await _manager.WriteAsync(path, writeOptions, transferData.Content).ConfigureAwait(false);
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
                        await _manager.CreateAsync(item.Key, createOptions).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
                else
                {
                    var newPath = item.Key;
                    if (transferData.Namespace != Namespace.Storage)
                    {
                        newPath = Path.Combine(path, item.Key);
                    }

                    var parentPath = PathHelper.GetParent(newPath);
                    if (!PathHelper.IsRootPath(parentPath))
                    {
                        await _manager.CreateAsync(parentPath, createOptions).ConfigureAwait(false);
                        await _manager.WriteAsync(newPath, writeOptions, item.Content).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
            }
        }
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, ConnectorOptions? options, 
        CancellationToken cancellationToken = default)
    {
        if (context.Connector is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var path = PathHelper.ToUnixPath(context.Entity);
        var listOptions = options.ToObject<ListOptions>();
        var storageEntities = await _manager.EntitiesAsync(path, listOptions);

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
                var content = await _manager.ReadAsync(entityItem.FullPath, readOptions).ConfigureAwait(false);
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
}