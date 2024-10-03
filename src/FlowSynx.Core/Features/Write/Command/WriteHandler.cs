using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Services;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Core.Parers.Contex;

namespace FlowSynx.Core.Features.Write.Command;

internal class WriteHandler : IRequestHandler<WriteRequest, Result<Unit>>
{
    private readonly ILogger<WriteHandler> _logger;
    private readonly IPluginService _storageService;
    private readonly IPluginContexParser _pluginContexParser;

    public WriteHandler(ILogger<WriteHandler> logger, IPluginService storageService, IPluginContexParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageService, nameof(storageService));
        _logger = logger;
        _storageService = storageService;
        _pluginContexParser = pluginInstanceParser;
    }

    public async Task<Result<Unit>> Handle(WriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContexParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            await _storageService.WriteAsync(contex, options, request.Data, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.WriteHandlerSuccessfullyWriten);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}