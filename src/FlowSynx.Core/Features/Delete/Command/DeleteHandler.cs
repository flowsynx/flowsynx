using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;
using FlowSynx.Core.Parers.Connector;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Delete.Command;

internal class DeleteHandler : IRequestHandler<DeleteRequest, Result<Unit>>
{
    private readonly ILogger<DeleteHandler> _logger;
    private readonly IConnectorParser _connectorParser;

    public DeleteHandler(ILogger<DeleteHandler> logger, IConnectorParser connectorParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _connectorParser = connectorParser;
    }

    public async Task<Result<Unit>> Handle(DeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var connectorContext = _connectorParser.Parse(request.Connector);
            var options = request.Options.ToConnectorOptions();
            var context = new Context(options, connectorContext.Next);
            await connectorContext.Current.DeleteAsync(context, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.DeleteHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}