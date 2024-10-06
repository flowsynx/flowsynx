using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Features.Read.Query;

internal class ReadHandler : IRequestHandler<ReadRequest, Result<ReadResult>>
{
    private readonly ILogger<ReadHandler> _logger;
    private readonly IPluginContextParser _pluginContextParser;

    public ReadHandler(ILogger<ReadHandler> logger, IPluginContextParser pluginContextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _pluginContextParser = pluginContextParser;
    }

    public async Task<Result<ReadResult>> Handle(ReadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContextParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            var response = await contex.InvokePlugin.ReadAsync(contex.Entity, contex.InferiorPlugin, options, cancellationToken);
            return await Result<ReadResult>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<ReadResult>.FailAsync(new List<string> { ex.Message });
        }
    }
}