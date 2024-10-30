using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using Microsoft.Extensions.Logging;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.IO.Serialization;
using FlowSynx.Data.Filter;
using FlowSynx.Connectors.Storage.Exceptions;
using FlowSynx.Connectors.Storage.Amazon.S3.Services;
using FlowSynx.Connectors.Storage.Amazon.S3.Models;

namespace FlowSynx.Connectors.Storage.Amazon.S3;

public class AmazonS3Connector : Connector
{
    private readonly ILogger<AmazonS3Connector> _logger;
    private readonly IDeserializer _deserializer;
    private readonly IDataFilter _dataFilter;
    private readonly IAmazonS3Connection _connection;
    private IAmazonS3Manager _manager = null!;
    private AmazonS3Specifications _s3Specifications = null!;

    public AmazonS3Connector(ILogger<AmazonS3Connector> logger, IDataFilter dataFilter,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataFilter, nameof(dataFilter));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataFilter = dataFilter;
        _deserializer = deserializer;
        _connection = new AmazonS3Connection();
    }

    public override Guid Id => Guid.Parse("b961131b-04cb-48df-9554-4252dc66c04c");
    public override string Name => "Amazon.S3";
    public override Namespace Namespace => Namespace.Storage;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(AmazonS3Specifications);

    public override Task Initialize()
    {
        _s3Specifications = Specifications.ToObject<AmazonS3Specifications>();
        var client = _connection.Connect(_s3Specifications);
        _manager = new AmazonS3Manager(_logger, client, _dataFilter, _deserializer);
        return Task.CompletedTask;
    }

    public override Task<object> About(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        throw new StorageException(Resources.AboutOperrationNotSupported);
    }

    public override async Task CreateAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var createOptions = context.Options.ToObject<CreateOptions>();

        await _manager.CreateAsync(pathOptions.Path, createOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task WriteAsync(Context context, object dataOptions, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var writeOptions = context.Options.ToObject<WriteOptions>();
        await _manager.WriteAsync(pathOptions.Path, writeOptions, dataOptions, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ReadResult> ReadAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var readOptions = context.Options.ToObject<ReadOptions>();

        return await _manager.ReadAsync(pathOptions.Path, readOptions, cancellationToken).ConfigureAwait(false);
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
        var deleteFilters = context.Options.ToObject<DeleteOptions>();

        var path = PathHelper.ToUnixPath(pathOptions.Path);

        var filteredEntities = await _manager.FilteredEntitiesAsync(path, listOptions, cancellationToken).ConfigureAwait(false);

        var entityItems = filteredEntities.ToList();
        if (!entityItems.Any())
            throw new StorageException(string.Format(Resources.NoFilesFoundWithTheGivenFilter, path));
        
        foreach (var entityItem in entityItems)
        {
            if (entityItem is not StorageEntity storageEntity)
                continue;

            await _manager.DeleteAsync(storageEntity.FullPath, cancellationToken).ConfigureAwait(false);
        }

        if (deleteFilters.Purge is true)
            await _manager.PurgeAsync(path, cancellationToken);
    }

    public override async Task<bool> ExistAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();

        return await _manager.ExistAsync(pathOptions.Path, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, 
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectorContext?.Current is not null)
            throw new StorageException(Resources.CalleeConnectorNotSupported);

        var pathOptions = context.Options.ToObject<PathOptions>();
        var listOptions = context.Options.ToObject<ListOptions>();

        return await _manager.FilteredEntitiesAsync(pathOptions.Path, listOptions, cancellationToken);
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
            sourceListOptions, sourceReadOptions, cancellationToken);

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
                await _manager.CreateAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
                await _manager.WriteAsync(path, writeOptions, transferData.Content, cancellationToken).ConfigureAwait(false);
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
                        await _manager.CreateAsync(item.Key, createOptions, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation($"Copy operation done for entity '{item.Key}'");
                    }
                }
                else
                {
                    var parentPath = PathHelper.GetParent(item.Key);
                    if (!PathHelper.IsRootPath(parentPath))
                    {
                        await _manager.CreateAsync(parentPath, createOptions, cancellationToken).ConfigureAwait(false);
                        await _manager.WriteAsync(item.Key, writeOptions, item.Content, cancellationToken).ConfigureAwait(false);
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

        var storageEntities = await _manager.EntitiesAsync(path, listOptions, cancellationToken);

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
                var content = await _manager.ReadAsync(entityItem.FullPath, readOptions, cancellationToken).ConfigureAwait(false);
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