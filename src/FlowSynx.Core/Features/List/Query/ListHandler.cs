using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Plugin.Abstractions.Extensions;

namespace FlowSynx.Core.Features.List.Query;

internal class ListHandler : IRequestHandler<ListRequest, Result<IEnumerable<object>>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IPluginContextParser _pluginContextParser;

    public ListHandler(ILogger<ListHandler> logger, IPluginContextParser pluginContextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _pluginContextParser = pluginContextParser;
    }

    public async Task<Result<IEnumerable<object>>> Handle(ListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContextParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            var response = await contex.InvokePlugin.ListAsync(contex.Entity, contex.InferiorPlugin, options, cancellationToken);
            return await Result<IEnumerable<object>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<object>>.FailAsync(new List<string> { ex.Message });
        }
    }
}