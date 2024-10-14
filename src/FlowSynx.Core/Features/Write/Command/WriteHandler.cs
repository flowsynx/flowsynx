using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Connectors.Abstractions.Extensions;

namespace FlowSynx.Core.Features.Write.Command;

internal class WriteHandler : IRequestHandler<WriteRequest, Result<Unit>>
{
    private readonly ILogger<WriteHandler> _logger;
    private readonly IContextParser _contexParser;

    public WriteHandler(ILogger<WriteHandler> logger, IContextParser contextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _contexParser = contextParser;
    }

    public async Task<Result<Unit>> Handle(WriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _contexParser.Parse(request.Entity);
            var options = request.Options.ToConnectorOptions();
            await contex.Connector.WriteAsync(contex.Context, options, request.Data, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.WriteHandlerSuccessfullyWriten);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}