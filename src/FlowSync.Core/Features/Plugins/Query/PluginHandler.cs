using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.Common.Models;
using FlowSync.Core.Plugins;
using EnsureThat;
using FlowSync.Abstractions;
using FlowSync.Core.Common;

namespace FlowSync.Core.Features.Plugins.Query;

internal class PluginHandler : IRequestHandler<PluginRequest, Result<IEnumerable<PluginResponse>>>
{
    private readonly ILogger<PluginHandler> _logger;
    private readonly IPluginsManager _pluginsManager;

    public PluginHandler(ILogger<PluginHandler> logger, IPluginsManager pluginsManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginsManager, nameof(pluginsManager));
        _logger = logger;
        _pluginsManager = pluginsManager;
    }

    public async Task<Result<IEnumerable<PluginResponse>>> Handle(PluginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<IPlugin> plugins;
            if (string.IsNullOrEmpty(request.Namespace))
                plugins = _pluginsManager.Plugins();
            else
            {
                var @namespace = EnumUtils.GetEnumValueOrDefault<PluginNamespace>(request.Namespace)!.Value;
                plugins = _pluginsManager.Plugins(@namespace);
            }

            var response = plugins.Select(x => new PluginResponse
            {
                Id = x.Id,
                Type = x.Type,
                Description = x.Description
            });

            return await Result<IEnumerable<PluginResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<PluginResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}