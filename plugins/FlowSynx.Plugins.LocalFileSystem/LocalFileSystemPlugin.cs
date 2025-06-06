﻿using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.LocalFileSystem.Models;
using FlowSynx.Plugins.LocalFileSystem.Services;

namespace FlowSynx.Plugins.LocalFileSystem;

public class LocalFileSystemPlugin : IPlugin
{
    private ILocalFileManager _manager = null!;
    private bool _isInitialized;

    public PluginMetadata Metadata => new()
    {
        Id = Guid.Parse("f6304870-0294-453e-9598-a82167ace653"),
        Name = "LocalFileSystem",
        Description = Resources.ConnectorDescription,
        Version = new PluginVersion(1, 0, 0),
        Namespace = PluginNamespace.Connectors,
        CompanyName = "FlowSynx",
        Authors = new List<string> { "FlowSynx" },
        Copyright = "© FlowSynx. All rights reserved.",
        Tags = new List<string>() { "FlowSynx", "Local", "LocalFileSystem" }
    };

    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(LocalFileSystemSpecifications);

    public Task Initialize(IPluginLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _manager = new LocalFileManager(logger);
        _isInitialized = true;
        return Task.CompletedTask;
    }

    public async Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");

        var operationParameter = parameters.ToObject<OperationParameter>();
        var operation = operationParameter.Operation;

        cancellationToken.ThrowIfCancellationRequested();

        switch (operation.ToLower())
        {
            case "create":
                await _manager.Create(parameters, cancellationToken).ConfigureAwait(false);
                return null;
            case "delete":
                await _manager.Delete(parameters, cancellationToken).ConfigureAwait(false);
                return null;
            case "exist":
                return await _manager.Exist(parameters, cancellationToken).ConfigureAwait(false);
            case "list":
                return await _manager.List(parameters, cancellationToken).ConfigureAwait(false);
            case "purge":
                await _manager.Purge(parameters, cancellationToken).ConfigureAwait(false);
                return null;
            case "read":
                return await _manager.Read(parameters, cancellationToken).ConfigureAwait(false);
            case "rename":
                await _manager.Rename(parameters, cancellationToken).ConfigureAwait(false);
                return null;
            case "write":
                await _manager.Write(parameters, cancellationToken).ConfigureAwait(false);
                return null;
            default:
                throw new NotSupportedException(string.Format(Resources.OperationIsNotSupported, operation));
        }
    }
}