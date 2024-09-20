using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Services;
using FlowSynx.Core.Parers.PluginInstancing;
using FlowSynx.Plugin.Abstractions.Extensions;

namespace FlowSynx.Core.Features.List.Query;

internal class ListHandler : IRequestHandler<ListRequest, Result<IEnumerable<object>>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IPluginService _storageService;
    private readonly IPluginInstanceParser _pluginInstanceParser;

    public ListHandler(ILogger<ListHandler> logger, IPluginService storageService, IPluginInstanceParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _pluginInstanceParser = pluginInstanceParser;
    }

    public async Task<Result<IEnumerable<object>>> Handle(ListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pluginInstance = _pluginInstanceParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            var response = await _storageService.ListAsync(pluginInstance, options, cancellationToken);
            return await Result<IEnumerable<object>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<object>>.FailAsync(new List<string> { ex.Message });
        }
    }
}