using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.PluginInstancing;
using FlowSynx.Plugin.Services;

namespace FlowSynx.Core.Features.Copy.Command;

internal class CopyHandler : IRequestHandler<CopyRequest, Result<IEnumerable<object>>>
{
    private readonly ILogger<CopyHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginInstanceParser _pluginInstanceParser;

    public CopyHandler(ILogger<CopyHandler> logger, IPluginService pluginService, 
        IPluginInstanceParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginService, nameof(pluginService));
        EnsureArg.IsNotNull(pluginInstanceParser, nameof(pluginInstanceParser));
        _logger = logger;
        _pluginService = pluginService;
        _pluginInstanceParser = pluginInstanceParser;
    }

    public async Task<Result<IEnumerable<object>>> Handle(CopyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var sourcePluginInstance = _pluginInstanceParser.Parse(request.SourceEntity);
            var destinationPluginInstance = _pluginInstanceParser.Parse(request.DestinationEntity);
            await _pluginService.CopyAsync(sourcePluginInstance, destinationPluginInstance, 
                request.Options, cancellationToken);
            return await Result<IEnumerable<object>>.SuccessAsync(Resources.CopyHandlerSuccessfullyCopy);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<object>>.FailAsync(new List<string> { ex.Message });
        }
    }
}