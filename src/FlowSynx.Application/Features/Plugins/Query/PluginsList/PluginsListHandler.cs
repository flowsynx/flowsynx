using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Services;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Plugin;

namespace FlowSynx.Application.Features.Plugins.Query.PluginsList;

internal class PluginsListHandler : IRequestHandler<PluginsListRequest, Result<IEnumerable<PluginsListResponse>>>
{
    private readonly ILogger<PluginsListHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly ICurrentUserService _currentUserService;

    public PluginsListHandler(
        ILogger<PluginsListHandler> logger, 
        IPluginService pluginService, 
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _pluginService = pluginService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IEnumerable<PluginsListResponse>>> Handle(PluginsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var plugins = await _pluginService.All(_currentUserService.UserId, cancellationToken);
            var response = plugins.Select(p => new PluginsListResponse
            {
                Id = p.Id,
                Type = p.Type,
                Version = p.Version,
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