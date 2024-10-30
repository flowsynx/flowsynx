using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Filter;
using FlowSynx.Connectors.Storage.Exceptions;
using FlowSynx.Connectors.Storage.LocalFileSystem.Models;
using FlowSynx.Connectors.Storage.LocalFileSystem.Services;
using FlowSynx.Connectors.Abstractions.Extensions;

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

    public override async Task<object> About(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);
        
        return await _manager.GetStatisticsAsync();
    }

    public override async Task CreateAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var createOptions = context.Options.ToObject<CreateOptions>();
        await _manager.CreateAsync(pathOptions.Path, createOptions).ConfigureAwait(false);
    }

    public override async Task WriteAsync(Context context, object dataOptions, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();
        await _manager.WriteAsync(pathOptions.Path, writeOptions, dataOptions).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var readOptions = context.Options.ToObject<ReadOptions>();
        return await _manager.ReadAsync(pathOptions.Path, readOptions).ConfigureAwait(false);
    }

    public override Task UpdateAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        var deleteOptions = context.Options.ToObject<DeleteOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);

        var entities = await _manager.FilteredEntitiesAsync(path, listOptions).ConfigureAwait(false);

        var entityItems = entities.ToList();
        if (!entityItems.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));
        
        foreach (var entityItem in entityItems)
        {
            if (entityItem is not StorageEntity storageEntity) 
                continue;

            await _manager.DeleteAsync(storageEntity.FullPath);
        }

        if (deleteOptions.Purge is true)
            await _manager.PurgeAsync(path);
    }

    public override Task<bool> ExistAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        return _manager.ExistAsync(pathOptions.Path);
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        return await _manager.FilteredEntitiesAsync(pathOptions.Path, listOptions);
    }

    public override async Task TransferAsync(Context sourceContext, Context destinationContext,
        CancellationToken cancellationToken = default)
    {
        if (destinationContext.ConnectorContext?.Current is null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var sourcePathOptions = sourceContext.Options.ToObject<PathOptions>();
        var sourceListOptions = sourceContext.Options.ToObject<ListOptions>();
        var sourceReadOptions = sourceContext.Options.ToObject<ReadOptions>();

        var transferData = await _manager.PrepareDataForTransferring(Namespace, Type, sourcePathOptions.Path,
            sourceListOptions, sourceReadOptions);

        var destinationPathOptions = destinationContext.Options.ToObject<PathOptions>();

        foreach (var row in transferData.Rows)
            row.Key = row.Key.Replace(sourcePathOptions.Path, destinationPathOptions.Path);

        await destinationContext.ConnectorContext.Current.ProcessTransferAsync(destinationContext, transferData, cancellationToken);
    }

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        CancellationToken cancellationToken = default)
    {
        var pathOptions = context.Options.ToObject<PathOptions>();
        var createOptions = context.Options.ToObject<CreateOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();

        var path = PathHelper.ToUnixPath(pathOptions.Path);

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

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();
        var path = PathHelper.ToUnixPath(pathOptions.Path);
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