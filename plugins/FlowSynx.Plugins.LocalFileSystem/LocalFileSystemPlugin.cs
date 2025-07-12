using FlowSynx.PluginCore;
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
        Description = Resources.PluginDescription,
        Version = new Version(1, 1, 1),
        Category = PluginCategory.Storage,
        CompanyName = "FlowSynx",
        Authors = new List<string> { "FlowSynx" },
        Copyright = "© FlowSynx. All rights reserved.",
        Tags = new List<string>() { "FlowSynx", "Local", "LocalFileSystem" },
        MinimumFlowSynxVersion = new Version(1, 1, 0),
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
        cancellationToken.ThrowIfCancellationRequested();

        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");

        var operationParameter = parameters.ToObject<OperationParameter>();
        var operation = operationParameter.Operation;

        if (OperationMap.TryGetValue(operation, out var handler))
        {
            return handler(parameters, cancellationToken);
        }

        throw new NotSupportedException(string.Format(Resources.OperationIsNotSupported, operation));
    }

    private Dictionary<string, Func<PluginParameters, CancellationToken, Task<object?>>> OperationMap => new(StringComparer.OrdinalIgnoreCase)
    {
        ["create"] = async (parameters, cancellationToken) => { await _manager.Create(parameters, cancellationToken); return null; },
        ["delete"] = async (parameters, cancellationToken) => { await _manager.Delete(parameters, cancellationToken); return null; },
        ["exist"] = async (parameters, cancellationToken) => await _manager.Exist(parameters, cancellationToken),
        ["list"] = async (parameters, cancellationToken) => await _manager.List(parameters, cancellationToken),
        ["purge"] = async (parameters, cancellationToken) => { await _manager.Purge(parameters, cancellationToken); return null; },
        ["read"] = async (parameters, cancellationToken) => await _manager.Read(parameters, cancellationToken),
        ["write"] = async (parameters, cancellationToken) => { await _manager.Write(parameters, cancellationToken); return null; },
    };

    public IReadOnlyCollection<string> SupportedOperations => OperationMap.Keys;
}