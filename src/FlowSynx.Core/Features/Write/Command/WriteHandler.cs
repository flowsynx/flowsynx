using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Core.Parers.Contex;

namespace FlowSynx.Core.Features.Write.Command;

internal class WriteHandler : IRequestHandler<WriteRequest, Result<Unit>>
{
    private readonly ILogger<WriteHandler> _logger;
    private readonly IPluginContextParser _pluginContexParser;

    public WriteHandler(ILogger<WriteHandler> logger, IPluginContextParser pluginContextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _pluginContexParser = pluginContextParser;
    }

    public async Task<Result<Unit>> Handle(WriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContexParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            await contex.InvokePlugin.WriteAsync(contex.Entity, contex.InferiorPlugin, options, request.Data, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.WriteHandlerSuccessfullyWriten);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}