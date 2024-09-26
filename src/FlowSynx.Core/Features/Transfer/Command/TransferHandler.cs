using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.PluginInstancing;
using FlowSynx.Plugin.Services;

namespace FlowSynx.Core.Features.Transfer.Command;

internal class TransferHandler : IRequestHandler<TransferRequest, Result<Unit>>
{
    private readonly ILogger<TransferHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginInstanceParser _pluginInstanceParser;

    public TransferHandler(ILogger<TransferHandler> logger, IPluginService pluginService, 
        IPluginInstanceParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginService, nameof(pluginService));
        EnsureArg.IsNotNull(pluginInstanceParser, nameof(pluginInstanceParser));
        _logger = logger;
        _pluginService = pluginService;
        _pluginInstanceParser = pluginInstanceParser;
    }

    public async Task<Result<Unit>> Handle(TransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var sourcePluginInstance = _pluginInstanceParser.Parse(request.SourceEntity);
            var destinationPluginInstance = _pluginInstanceParser.Parse(request.DestinationEntity);
            await _pluginService.TransferAsync(sourcePluginInstance, destinationPluginInstance, 
                request.Options, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.CopyHandlerSuccessfullyCopy);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}