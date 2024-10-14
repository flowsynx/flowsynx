using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Connectors.Abstractions.Extensions;

namespace FlowSynx.Core.Features.About.Query;

internal class AboutHandler : IRequestHandler<AboutRequest, Result<object>>
{
    private readonly ILogger<AboutHandler> _logger;
    private readonly IContextParser _contextParser;

    public AboutHandler(ILogger<AboutHandler> logger, IContextParser contextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _contextParser = contextParser;
    }

    public async Task<Result<object>> Handle(AboutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _contextParser.Parse(request.Entity);
            var options = request.Options.ToConnectorOptions();
            var response = await contex.Connector.About(contex.Context, options, cancellationToken);
            return await Result<object>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}