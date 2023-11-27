using MediatR;
using Microsoft.Extensions.Logging;
using FlowSync.Core.Common.Models;
using FlowSync.Core.Plugins;
using EnsureThat;

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
            var result = _pluginsManager.Plugins();

            var response = result.Select(x => new PluginResponse
            {
                Id = x.Id,
                Namespace = x.Namespace,
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