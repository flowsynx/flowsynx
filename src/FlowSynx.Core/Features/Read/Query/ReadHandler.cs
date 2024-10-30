using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Core.Parers.Connector;

namespace FlowSynx.Core.Features.Read.Query;

internal class ReadHandler : IRequestHandler<ReadRequest, Result<ReadResult>>
{
    private readonly ILogger<ReadHandler> _logger;
    private readonly IConnectorParser _connectorParser;

    public ReadHandler(ILogger<ReadHandler> logger, IConnectorParser connectorParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _connectorParser = connectorParser;
    }

    public async Task<Result<ReadResult>> Handle(ReadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var connectorContext = _connectorParser.Parse(request.Connector);
            var options = request.Options.ToConnectorOptions();
            var context = new Context(options, connectorContext.Next);
            var response = await connectorContext.Current.ReadAsync(context, cancellationToken);
            return await Result<ReadResult>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<ReadResult>.FailAsync(new List<string> { ex.Message });
        }
    }
}