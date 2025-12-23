using FlowSynx.Application.PluginHost.Manager;
using FlowSynx.Domain.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Plugins.Query.PluginsFullDetailsList;

public sealed class PluginsFullDetailsListHandler
    : IRequestHandler<PluginsFullDetailsListRequest, PaginatedResult<PluginsFullDetailsListResponse>>
{
    private readonly ILogger<PluginsFullDetailsListHandler> _logger;
    private readonly IPluginManager _pluginManager;

    public PluginsFullDetailsListHandler(
        ILogger<PluginsFullDetailsListHandler> logger, 
        IPluginManager pluginManager)
    {
        _logger = logger;
        _pluginManager = pluginManager;
    }

    public async Task<PaginatedResult<PluginsFullDetailsListResponse>> Handle(
        PluginsFullDetailsListRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var (items, totalCount) = await _pluginManager.GetPluginsFullDetailsListAsync(
                request.Page,
                request.PageSize,
                cancellationToken);

            var data = items.Select(p => new PluginsFullDetailsListResponse
            {
                Type = p.Type,
                CategoryTitle = p.CategoryTitle,
                Description = p.Description,
                Versions = p.Versions,
                LatestVersion = p.LatestVersion,
                Registry = p.Registry
            }).ToList();

            return await PaginatedResult<PluginsFullDetailsListResponse>.SuccessAsync(
                data,
                request.Page,
                request.PageSize,
                totalCount);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, ex.Message);
            return await PaginatedResult<PluginsFullDetailsListResponse>.FailureAsync(ex.Message);
        }
    }
}