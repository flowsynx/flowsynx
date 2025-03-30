using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Application.Features.Plugins.Query.List;

internal class PluginsListHandler : IRequestHandler<PluginsListRequest, Result<IEnumerable<PluginsListResponse>>>
{
    private readonly ILogger<PluginsListHandler> _logger;
    private readonly IPluginService _pluginService;

    public PluginsListHandler(ILogger<PluginsListHandler> logger, IPluginService pluginService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginService);
        _logger = logger;
        _pluginService = pluginService;
    }

    public async Task<Result<IEnumerable<PluginsListResponse>>> Handle(PluginsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var plugins = await _pluginService.All(cancellationToken);
            var response = plugins.Select(p => new PluginsListResponse
            {
                Id = p.Id,
                Type = p.Type,
                Description = p.Description,
            });
            return await Result<IEnumerable<PluginsListResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<PluginsListResponse>>.FailAsync(ex.ToString());
        }
    }
}