using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Services;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Core.Parers.PluginInstancing;

namespace FlowSynx.Core.Features.Write.Command;

internal class WriteHandler : IRequestHandler<WriteRequest, Result<Unit>>
{
    private readonly ILogger<WriteHandler> _logger;
    private readonly IPluginService _storageService;
    private readonly IPluginInstanceParser _pluginInstanceParser;

    public WriteHandler(ILogger<WriteHandler> logger, IPluginService storageService, IPluginInstanceParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _pluginInstanceParser = pluginInstanceParser;
    }

    public async Task<Result<Unit>> Handle(WriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pluginInstance = _pluginInstanceParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            await _storageService.WriteAsync(pluginInstance, options, request.Data, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.WriteHandlerSuccessfullyWriten);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}