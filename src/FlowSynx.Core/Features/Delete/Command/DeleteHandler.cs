using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Services;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Plugin.Abstractions.Extensions;

namespace FlowSynx.Core.Features.Delete.Command;

internal class DeleteHandler : IRequestHandler<DeleteRequest, Result<Unit>>
{
    private readonly ILogger<DeleteHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginContexParser _pluginContexParser;

    public DeleteHandler(ILogger<DeleteHandler> logger, IPluginService pluginService, IPluginContexParser pluginContexParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginService, nameof(pluginService));
        _logger = logger;
        _pluginService = pluginService;
        _pluginContexParser = pluginContexParser;
    }

    public async Task<Result<Unit>> Handle(DeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContexParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            await _pluginService.DeleteAsync(contex, options, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.DeleteHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}