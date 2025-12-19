using FlowSynx.Application.PluginHost.Manager;
using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Plugins.Query.PluginsRegistriesList;

public sealed class PluginsRegistriesListHandler
    : IRequestHandler<PluginsRegistriesListRequest, PaginatedResult<PluginsRegistriesListResponse>>
{
    private readonly IPluginManager _pluginManager;

    public PluginsRegistriesListHandler(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public async Task<PaginatedResult<PluginsRegistriesListResponse>> Handle(
        PluginsRegistriesListRequest request,
        CancellationToken cancellationToken)
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

        return PaginatedResult<PluginsRegistriesListResponse>.Success(
            data,
            request.Page,
            request.PageSize,
            totalCount);
    }
}