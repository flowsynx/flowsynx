﻿using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Connectors.Storage.LocalFileSystem.Models;
using FlowSynx.Connectors.Storage.LocalFileSystem.Services;
using FlowSynx.Data.Queries;

namespace FlowSynx.Connectors.Storage.LocalFileSystem;

public class LocalFileSystemConnector : Connector
{
    private readonly ILogger<LocalFileSystemConnector> _logger;
    private readonly IDataService _dataService;
    private readonly IDeserializer _deserializer;
    private ILocalFileManager _manager = null!;

    public LocalFileSystemConnector(ILogger<LocalFileSystemConnector> logger, IDataService dataService,
        IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(dataService, nameof(dataService));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
        _logger = logger;
        _dataService = dataService;
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
        _manager = new LocalFileManager(_logger, _dataService, _deserializer);
        return Task.CompletedTask;
    }

    public override async Task<object> About(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.About(context).ConfigureAwait(false);

    public override async Task CreateAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.CreateAsync(context).ConfigureAwait(false);

    public override async Task WriteAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.WriteAsync(context).ConfigureAwait(false);

    public override async Task<ReadResult> ReadAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.ReadAsync(context).ConfigureAwait(false);

    public override async Task UpdateAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.UpdateAsync(context).ConfigureAwait(false);

    public override async Task DeleteAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.DeleteAsync(context).ConfigureAwait(false);

    public override async Task<bool> ExistAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.ExistAsync(context);

    public override async Task<IEnumerable<object>> ListAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.FilteredEntitiesAsync(context).ConfigureAwait(false);

    public override async Task TransferAsync(Context sourceContext, Context destinationContext, 
        TransferKind transferKind, CancellationToken cancellationToken = default) =>
        await _manager.TransferAsync(Namespace, Type, sourceContext, destinationContext, transferKind, 
            cancellationToken).ConfigureAwait(false);

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        TransferKind transferKind, CancellationToken cancellationToken = default) =>
        await _manager.ProcessTransferAsync(context, transferData, transferKind, cancellationToken).ConfigureAwait(false);

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context, 
        CancellationToken cancellationToken = default) =>
        await _manager.CompressAsync(context, cancellationToken).ConfigureAwait(false);
}