using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Core.Parers.Connector;

namespace FlowSynx.Core.Features.Write.Command;

internal class WriteHandler : IRequestHandler<WriteRequest, Result<Unit>>
{
    private readonly ILogger<WriteHandler> _logger;
    private readonly IConnectorParser _connectorParser;

    public WriteHandler(ILogger<WriteHandler> logger, IConnectorParser connectorParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _connectorParser = connectorParser;
    }

    public async Task<Result<Unit>> Handle(WriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            //var connectorContext = _connectorParser.Parse(request.Connector);
            //var options = request.Options.ToConnectorOptions();
            //var context = new Context(options, connectorContext.Next);
            //await connectorContext.Current.WriteAsync(context, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.WriteHandlerSuccessfullyWriten);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}