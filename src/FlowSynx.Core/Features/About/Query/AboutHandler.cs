using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Plugin.Services;
using FlowSynx.Core.Parers.PluginInstancing;

namespace FlowSynx.Core.Features.About.Query;

internal class AboutHandler : IRequestHandler<AboutRequest, Result<object>>
{
    private readonly ILogger<AboutHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginInstanceParser _pluginInstanceParser;

    public AboutHandler(ILogger<AboutHandler> logger, IPluginService pluginService, IPluginInstanceParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginService, nameof(pluginService));
        _logger = logger;
        _pluginService = pluginService;
        _pluginInstanceParser = pluginInstanceParser;
    }

    public async Task<Result<object>> Handle(AboutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var storageNorms = _pluginInstanceParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            var response = await _pluginService.About(storageNorms, options, cancellationToken);
            return await Result<object>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}