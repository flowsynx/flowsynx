using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Core.Parers.Connector;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Exist.Query;

internal class ExistHandler : IRequestHandler<ExistRequest, Result<object>>
{
    private readonly ILogger<ExistHandler> _logger;
    private readonly IConnectorParser _connectorParser;

    public ExistHandler(ILogger<ExistHandler> logger, IConnectorParser connectorParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _connectorParser = connectorParser;
    }

    public async Task<Result<object>> Handle(ExistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            //var connectorContext = _connectorParser.Parse(request.Connector);
            //var options = request.Options.ToConnectorOptions();
            //var context = new Context(options, connectorContext.Next);
            //var response = await connectorContext.Current.ExistAsync(context, cancellationToken);
            var response = "amin";
            return await Result<object>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}