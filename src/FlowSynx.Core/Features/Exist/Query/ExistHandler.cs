using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Plugin.Abstractions.Extensions;

namespace FlowSynx.Core.Features.Exist.Query;

internal class ExistHandler : IRequestHandler<ExistRequest, Result<object>>
{
    private readonly ILogger<ExistHandler> _logger;
    private readonly IPluginContextParser _pluginContextParser;

    public ExistHandler(ILogger<ExistHandler> logger, IPluginContextParser pluginContextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _pluginContextParser = pluginContextParser;
    }

    public async Task<Result<object>> Handle(ExistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContextParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            var response = await contex.InvokePlugin.ExistAsync(contex.Entity, contex.InferiorPlugin, options, cancellationToken);
            return await Result<object>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}