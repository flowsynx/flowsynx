using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Connectors.Abstractions.Extensions;

namespace FlowSynx.Core.Features.Delete.Command;

internal class DeleteHandler : IRequestHandler<DeleteRequest, Result<Unit>>
{
    private readonly ILogger<DeleteHandler> _logger;
    private readonly IContextParser _contextParser;

    public DeleteHandler(ILogger<DeleteHandler> logger, IContextParser contextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _contextParser = contextParser;
    }

    public async Task<Result<Unit>> Handle(DeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _contextParser.Parse(request.Connector);
            var options = request.Options.ToConnectorOptions();
            await contex.Connector.DeleteAsync(contex.Context, options, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.DeleteHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}