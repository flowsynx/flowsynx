using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Services;
using FlowSynx.Core.Parers.Contex;

namespace FlowSynx.Core.Features.Create.Command;

internal class CreateHandler : IRequestHandler<CreateRequest, Result<Unit>>
{
    private readonly ILogger<CreateHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginContexParser _pluginContexParser;

    public CreateHandler(ILogger<CreateHandler> logger, IPluginService pluginService, IPluginContexParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginService, nameof(pluginService));
        _logger = logger;
        _pluginService = pluginService;
        _pluginContexParser = pluginInstanceParser;
    }

    public async Task<Result<Unit>> Handle(CreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContexParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            await _pluginService.CreateAsync(contex, options, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.CreateHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}