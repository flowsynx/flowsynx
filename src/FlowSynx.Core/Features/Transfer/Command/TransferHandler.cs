using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;

namespace FlowSynx.Core.Features.Transfer.Command;

internal class TransferHandler : IRequestHandler<TransferRequest, Result<Unit>>
{
    private readonly ILogger<TransferHandler> _logger;
    private readonly IPluginContextParser _pluginContextParser;

    public TransferHandler(ILogger<TransferHandler> logger, IPluginContextParser pluginContextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginContextParser, nameof(pluginContextParser));
        _logger = logger;
        _pluginContextParser = pluginContextParser;
    }

    public async Task<Result<Unit>> Handle(TransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContextParser.Parse(request.SourceEntity);
            var destinationPluginContext = _pluginContextParser.Parse(request.DestinationEntity);
            await contex.InvokePlugin.TransferAsync(contex.Entity, contex.InferiorPlugin, request.Options, null, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.CopyHandlerSuccessfullyCopy);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}