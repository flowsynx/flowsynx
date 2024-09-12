using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Services;
using FlowSynx.Core.Parers.PluginInstancing;

namespace FlowSynx.Core.Features.Create.Command;

internal class CreateHandler : IRequestHandler<CreateRequest, Result<object>>
{
    private readonly ILogger<CreateHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginInstanceParser _pluginInstanceParser;

    public CreateHandler(ILogger<CreateHandler> logger, IPluginService pluginService, IPluginInstanceParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginService, nameof(pluginService));
        _logger = logger;
        _pluginService = pluginService;
        _pluginInstanceParser = pluginInstanceParser;
    }

    public async Task<Result<object>> Handle(CreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var pluginInstance = _pluginInstanceParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            var response = await _pluginService.CreateAsync(pluginInstance, options, cancellationToken);
            return await Result<object>.SuccessAsync(response, Resources.MakeDirectoryHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}