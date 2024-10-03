using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Plugin.Services;

namespace FlowSynx.Core.Features.Transfer.Command;

internal class TransferHandler : IRequestHandler<TransferRequest, Result<Unit>>
{
    private readonly ILogger<TransferHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginContexParser _pluginContexParser;

    public TransferHandler(ILogger<TransferHandler> logger, IPluginService pluginService,
        IPluginContexParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginService, nameof(pluginService));
        EnsureArg.IsNotNull(pluginInstanceParser, nameof(pluginInstanceParser));
        _logger = logger;
        _pluginService = pluginService;
        _pluginContexParser = pluginInstanceParser;
    }

    public async Task<Result<Unit>> Handle(TransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContexParser.Parse(request.SourceEntity);
            var destinationPluginInstance = _pluginContexParser.Parse(request.DestinationEntity);
            await _pluginService.TransferAsync(contex, destinationPluginInstance, 
                request.Options, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.CopyHandlerSuccessfullyCopy);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}