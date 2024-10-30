using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Core.Parers.Connector;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.List.Query;

internal class ListHandler : IRequestHandler<ListRequest, Result<IEnumerable<object>>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IConnectorParser _connectorParser;

    public ListHandler(ILogger<ListHandler> logger, IConnectorParser connectorParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _connectorParser = connectorParser;
    }

    public async Task<Result<IEnumerable<object>>> Handle(ListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var connectorContext = _connectorParser.Parse(request.Connector);
            var options = request.Options.ToConnectorOptions();
            var context = new Context(options, connectorContext.Next);
            var response = await connectorContext.Current.ListAsync(context, cancellationToken);
            return await Result<IEnumerable<object>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<object>>.FailAsync(new List<string> { ex.Message });
        }
    }
}