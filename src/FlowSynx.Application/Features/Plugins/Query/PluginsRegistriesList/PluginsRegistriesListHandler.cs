using FlowSynx.Application.PluginHost.Manager;
using FlowSynx.Domain.Wrapper;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Plugins.Query.PluginsRegistriesList;

public sealed class PluginsRegistriesListHandler
    : IRequestHandler<PluginsRegistriesListRequest, PaginatedResult<PluginsRegistriesListResponse>>
{
    private readonly ILogger<PluginsRegistriesListHandler> _logger;
    private readonly IPluginManager _pluginManager;

    public PluginsRegistriesListHandler(
        ILogger<PluginsRegistriesListHandler> logger, 
        IPluginManager pluginManager)
    {
        _logger = logger;
        _pluginManager = pluginManager;
    }

    public async Task<PaginatedResult<PluginsRegistriesListResponse>> Handle(
        PluginsRegistriesListRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Assumes IPluginManager can enumerate plugins available across all configured registries.
            // The manager should return items and a total count for pagination.
            var (items, totalCount) = await _pluginManager.GetRegistryPluginsAsync(
                request.Page,
                request.PageSize,
                cancellationToken);

            var data = items.Select(p => new PluginsRegistriesListResponse
            {
                Type = p.Type,
                CategoryTitle = p.CategoryTitle,
                Description = p.Description,
                Version = p.Version,
                Registry = p.Registry
            }).ToList();

            return await PaginatedResult<PluginsRegistriesListResponse>.SuccessAsync(
                data,
                request.Page,
                request.PageSize,
                totalCount);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, ex.Message);
            return await PaginatedResult<PluginsRegistriesListResponse>.FailureAsync(ex.Message);
        }
    }
}