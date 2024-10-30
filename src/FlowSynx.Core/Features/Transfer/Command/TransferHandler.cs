using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Connector;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;

namespace FlowSynx.Core.Features.Transfer.Command;

internal class TransferHandler : IRequestHandler<TransferRequest, Result<Unit>>
{
    private readonly ILogger<TransferHandler> _logger;
    private readonly IConnectorParser _connectorParser;

    public TransferHandler(ILogger<TransferHandler> logger, IConnectorParser connectorParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(connectorParser, nameof(connectorParser));
        _logger = logger;
        _connectorParser = connectorParser;
    }

    public async Task<Result<Unit>> Handle(TransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var sourceConnectorContext = _connectorParser.Parse(request.Source.Connector);
            var sourceOptions = request.Source.Options.ToConnectorOptions();
            var sourceContext = new Context(sourceOptions, sourceConnectorContext.Next);

            var destinationConnectorContext = _connectorParser.Parse(request.Destination.Connector);
            var destinationOptions = request.Destination.Options.ToConnectorOptions();
            var destinationContext = new Context(destinationOptions, destinationConnectorContext);

            await sourceConnectorContext.Current.TransferAsync(sourceContext, destinationContext,cancellationToken);

            return await Result<Unit>.SuccessAsync(Resources.CopyHandlerSuccessfullyCopy);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}