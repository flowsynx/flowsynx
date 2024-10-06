using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Core.Parers.Contex;

namespace FlowSynx.Core.Features.Create.Command;

internal class CreateHandler : IRequestHandler<CreateRequest, Result<Unit>>
{
    private readonly ILogger<CreateHandler> _logger;
    private readonly IPluginContextParser _pluginContextParser;

    public CreateHandler(ILogger<CreateHandler> logger, IPluginContextParser pluginContextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _pluginContextParser = pluginContextParser;
    }

    public async Task<Result<Unit>> Handle(CreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContextParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            await contex.InvokePlugin.CreateAsync(contex.Entity, contex.InferiorPlugin, options, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.CreateHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}