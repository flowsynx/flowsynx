using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Services;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Core.Parers.Contex;

namespace FlowSynx.Core.Features.Read.Query;

internal class ReadHandler : IRequestHandler<ReadRequest, Result<object>>
{
    private readonly ILogger<ReadHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginContexParser _pluginContexParser;

    public ReadHandler(ILogger<ReadHandler> logger, IPluginService pluginService, IPluginContexParser pluginContexParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginService, nameof(pluginService));
        _logger = logger;
        _pluginService = pluginService;
        _pluginContexParser = pluginContexParser;
    }

    public async Task<Result<object>> Handle(ReadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContexParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            var response = await _pluginService.ReadAsync(contex, options, cancellationToken);
            return await Result<object>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}