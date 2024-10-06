using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Plugin.Abstractions.Extensions;

namespace FlowSynx.Core.Features.Delete.Command;

internal class DeleteHandler : IRequestHandler<DeleteRequest, Result<Unit>>
{
    private readonly ILogger<DeleteHandler> _logger;
    private readonly IPluginContextParser _pluginContextParser;

    public DeleteHandler(ILogger<DeleteHandler> logger, IPluginContextParser pluginContextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _pluginContextParser = pluginContextParser;
    }

    public async Task<Result<Unit>> Handle(DeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContextParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            await contex.InvokePlugin.DeleteAsync(contex.Entity, contex.InferiorPlugin, options, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.DeleteHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}