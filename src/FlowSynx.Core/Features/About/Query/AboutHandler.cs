using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using FlowSynx.Core.Parers.Contex;

namespace FlowSynx.Core.Features.About.Query;

internal class AboutHandler : IRequestHandler<AboutRequest, Result<object>>
{
    private readonly ILogger<AboutHandler> _logger;
    private readonly IPluginContextParser _pluginContextParser;

    public AboutHandler(ILogger<AboutHandler> logger, IPluginContextParser pluginContextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _pluginContextParser = pluginContextParser;
    }

    public async Task<Result<object>> Handle(AboutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContextParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            var response = await contex.InvokePlugin.About(contex.InferiorPlugin, options, cancellationToken);
            return await Result<object>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}