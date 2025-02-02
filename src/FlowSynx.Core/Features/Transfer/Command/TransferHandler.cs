using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Connector;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Commons;

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
            //var transferKind = string.IsNullOrEmpty(request.TransferKind)
            //    ? TransferKind.Copy
            //    : EnumUtils.GetEnumValueOrDefault<TransferKind>(request.TransferKind)!.Value;

            //var sourceConnectorContext = _connectorParser.Parse(request.From.Connector);
            //var sourceOptions = request.From.Options.ToConnectorOptions();
            //var sourceContext = new Context(sourceOptions, sourceConnectorContext.Next);

            //var destinationConnectorContext = _connectorParser.Parse(request.To.Connector);
            //var destinationOptions = request.To.Options.ToConnectorOptions();
            //var destinationContext = new Context(destinationOptions, destinationConnectorContext);

            //await sourceConnectorContext.Current.TransferAsync(sourceContext, cancellationToken);

            return await Result<Unit>.SuccessAsync(Resources.CopyHandlerSuccessfullyCopy);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}