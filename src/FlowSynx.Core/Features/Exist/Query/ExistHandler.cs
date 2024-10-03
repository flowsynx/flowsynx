using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Plugin.Services;
using FlowSynx.Plugin.Abstractions.Extensions;

namespace FlowSynx.Core.Features.Exist.Query;

internal class ExistHandler : IRequestHandler<ExistRequest, Result<object>>
{
    private readonly ILogger<ExistHandler> _logger;
    private readonly IPluginService _pluginService;
    private readonly IPluginContexParser _pluginContexParser;

    public ExistHandler(ILogger<ExistHandler> logger, IPluginService pluginService, IPluginContexParser pluginInstanceParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginService, nameof(pluginService));
        _logger = logger;
        _pluginService = pluginService;
        _pluginContexParser = pluginInstanceParser;
    }

    public async Task<Result<object>> Handle(ExistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _pluginContexParser.Parse(request.Entity);
            var options = request.Options.ToPluginFilters();
            var response = await _pluginService.ExistAsync(contex, options, cancellationToken);
            return await Result<object>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}