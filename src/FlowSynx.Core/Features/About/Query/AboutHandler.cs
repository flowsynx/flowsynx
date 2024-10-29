using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Core.Parers.Connector;

namespace FlowSynx.Core.Features.About.Query;

internal class AboutHandler : IRequestHandler<AboutRequest, Result<object>>
{
    private readonly ILogger<AboutHandler> _logger;
    private readonly IConnectorParser _connectorParser;

    public AboutHandler(ILogger<AboutHandler> logger, IConnectorParser connectorParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _connectorParser = connectorParser;
    }

    public async Task<Result<object>> Handle(AboutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var connectorContext = _connectorParser.Parse(request.Connector);
            var options = request.Options.ToConnectorOptions();
            var context = new Context(options, connectorContext);
            var response = await connectorContext.Current.About(context, cancellationToken);
            return await Result<object>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}