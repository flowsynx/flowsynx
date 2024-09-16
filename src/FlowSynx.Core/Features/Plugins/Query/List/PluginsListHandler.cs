using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Manager;
using FlowSynx.Plugin.Manager.Options;

namespace FlowSynx.Core.Features.Plugins.Query.List;

internal class PluginsListHandler : IRequestHandler<PluginsListRequest, Result<IEnumerable<object>>>
{
    private readonly ILogger<PluginsListHandler> _logger;
    private readonly IPluginsManager _pluginsManager;

    public PluginsListHandler(ILogger<PluginsListHandler> logger, IPluginsManager pluginsManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginsManager, nameof(pluginsManager));
        _logger = logger;
        _pluginsManager = pluginsManager;
    }

    public async Task<Result<IEnumerable<object>>> Handle(PluginsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var listOptions = new PluginListOptions()
            {
                Fields = request.Fields ?? [],
                Filter = request.Filter ?? string.Empty,
                CaseSensitive = request.CaseSensitive ?? false,
                Sort = request.Sort ?? string.Empty,
                Limit = request.Limit ?? string.Empty
            };

            var response = _pluginsManager.List(listOptions);
            return await Result<IEnumerable<object>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<object>>.FailAsync(new List<string> { ex.Message });
        }
    }
}