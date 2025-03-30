using Microsoft.Extensions.DependencyInjection;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore;
using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;

namespace FlowSynx.Infrastructure.Services;

public class PluginService : IPluginService
{
    private readonly ILogger<PluginService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PluginService(ILogger<PluginService> logger, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task<IReadOnlyCollection<Plugin>> All(CancellationToken cancellationToken)
    {
        var plugins = Plugins().ToList();
        return Task.FromResult<IReadOnlyCollection<Plugin>>(plugins);
    }

    public Task<Plugin> Get(Guid pluginId, CancellationToken cancellationToken)
    {
        var result = Plugins().FirstOrDefault(x => x.Id == pluginId);

        if (result != null)
        {
            var activatePlugin = (Plugin)ActivatorUtilities.CreateInstance(_serviceProvider, result.GetType());
            return Task.FromResult(activatePlugin);
        }

        var exception = new FlowSynxException((int)ErrorCode.PluginNotFound, $"Plugin with id '{pluginId}' could not found!");
        _logger.LogError(exception.ToString());
        throw exception;
    }

    public Task<Plugin> Get(string type, CancellationToken cancellationToken)
    {
        var result = Plugins().FirstOrDefault(x => x.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        if (result != null)
        {
            var activatePlugin = (Plugin)ActivatorUtilities.CreateInstance(_serviceProvider, result.GetType());
            return Task.FromResult(activatePlugin);
        }

        var message = string.Format(Resources.PluginServiceCouldNotFoundPlugin, type);
        var exception = new FlowSynxException((int)ErrorCode.PluginTypeNotFound, message);
        _logger.LogError(exception.ToString());
        throw exception;
    }

    public async Task<bool> IsExist(string type, CancellationToken cancellationToken)
    {
        try
        {
            var plugin = await Get(type, cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            Plugins().ToList();
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private IEnumerable<Plugin> Plugins() => _serviceProvider.GetServices<Plugin>();
}