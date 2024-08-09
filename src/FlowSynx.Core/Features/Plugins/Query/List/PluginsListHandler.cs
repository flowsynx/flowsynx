using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Commons;
using FlowSynx.Plugin;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Options;

namespace FlowSynx.Core.Features.Plugins.Query.List;

internal class PluginsListHandler : IRequestHandler<PluginsListRequest, Result<IEnumerable<PluginsListResponse>>>
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

    public async Task<Result<IEnumerable<PluginsListResponse>>> Handle(PluginsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var searchOptions = new PluginSearchOptions()
            {
                Include = request.Include,
                Exclude = request.Exclude,
                CaseSensitive = request.CaseSensitive ?? false
            };

            var listOptions = new PluginListOptions()
            {
                Sorting = request.Sorting,
                MaxResult = request.MaxResults
            };

            var result = _pluginsManager.List(searchOptions, listOptions);
            var response = result.Select(x => new PluginsListResponse
            {
                Id = x.Id,
                Type = x.Type,
                Description = x.Description
            });

            return await Result<IEnumerable<PluginsListResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<PluginsListResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}