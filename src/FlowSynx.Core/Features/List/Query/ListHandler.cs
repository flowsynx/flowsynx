using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Core.Parers.Connector;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;

namespace FlowSynx.Core.Features.List.Query;

internal class ListHandler : IRequestHandler<ListRequest, Result<InterchangeData>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IConnectorParser _connectorParser;

    public ListHandler(ILogger<ListHandler> logger, IConnectorParser connectorParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _connectorParser = connectorParser;
    }

    public async Task<Result<InterchangeData>> Handle(ListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            //var connectorContext = _connectorParser.Parse(request.Connector);
            //var options = request.Options.ToConnectorOptions();
            //var context = new Context(options, connectorContext.Next);
            //var response = await connectorContext.Current.ListAsync(context, cancellationToken);
            var response = "s";
            return await Result<InterchangeData>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<InterchangeData>.FailAsync(new List<string> { ex.Message });
        }
    }
}