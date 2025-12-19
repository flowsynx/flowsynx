using FlowSynx.Application.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Plugin;

namespace FlowSynx.Application.Features.Plugins.Query.PluginsList;

internal class PluginsListHandler : IRequestHandler<PluginsListRequest, PaginatedResult<PluginsListResponse>>
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

    public async Task<PaginatedResult<PluginsListResponse>> Handle(PluginsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var plugins = await _pluginService.All(_currentUserService.UserId(), cancellationToken);
            var response = plugins.Select(p => new PluginsListResponse
            {
                Id = p.Id,
                Type = p.Type,
                Version = p.Version,
                Description = p.Description,
            });
            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);
            return await PaginatedResult<PluginsListResponse>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, ex.Message);
            return await PaginatedResult<PluginsListResponse>.FailureAsync(ex.Message);
        }
    }
}

