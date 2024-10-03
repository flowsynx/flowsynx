using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Services;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Plugin.Abstractions.Extensions;

namespace FlowSynx.Core.Features.List.Query;

internal class ListHandler : IRequestHandler<ListRequest, Result<IEnumerable<object>>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IPluginService _storageService;
    private readonly IPluginContexParser _pluginContexParser;

    public ListHandler(ILogger<ListHandler> logger, IPluginService storageService, IPluginContexParser pluginContexParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _pluginContexParser = pluginContexParser;
    }

    public async Task<Result<IEnumerable<object>>> Handle(ListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContexParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            var response = await _storageService.ListAsync(contex, options, cancellationToken);
            return await Result<IEnumerable<object>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<object>>.FailAsync(new List<string> { ex.Message });
        }
    }
}