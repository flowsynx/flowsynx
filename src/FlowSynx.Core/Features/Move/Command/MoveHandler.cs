using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.PluginInstancing;
using FlowSynx.Plugin.Services;

namespace FlowSynx.Core.Features.Move.Command;

internal class MoveHandler : IRequestHandler<MoveRequest, Result<Unit>>
{
    private readonly ILogger<MoveHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginInstanceParser _pluginInstanceParser;

    public MoveHandler(ILogger<MoveHandler> logger, IPluginService pluginService, 
        IPluginInstanceParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginService, nameof(pluginService));
        EnsureArg.IsNotNull(pluginInstanceParser, nameof(pluginInstanceParser));
        _logger = logger;
        _pluginService = pluginService;
        _pluginInstanceParser = pluginInstanceParser;
    }

    public async Task<Result<Unit>> Handle(MoveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var sourcePluginInstance = _pluginInstanceParser.Parse(request.SourceEntity);
            var destinationPluginInstance = _pluginInstanceParser.Parse(request.DestinationEntity);
            await _pluginService.MoveAsync(sourcePluginInstance, destinationPluginInstance,
                request.Options, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.MoveHandlerSuccessfullyMoved);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}