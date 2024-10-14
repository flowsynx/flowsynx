using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;

namespace FlowSynx.Core.Features.Transfer.Command;

internal class TransferHandler : IRequestHandler<TransferRequest, Result<Unit>>
{
    private readonly ILogger<TransferHandler> _logger;
    private readonly IContextParser _contextParser;

    public TransferHandler(ILogger<TransferHandler> logger, IContextParser contextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(contextParser, nameof(contextParser));
        _logger = logger;
        _contextParser = contextParser;
    }

    public async Task<Result<Unit>> Handle(TransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var sourceContext = _contextParser.Parse(request.SourceEntity);
            var destinationContext = _contextParser.Parse(request.DestinationEntity);

            await sourceContext.Connector.TransferAsync(sourceContext.Context, destinationContext.Connector,
                destinationContext.Context, request.Options, cancellationToken);

            return await Result<Unit>.SuccessAsync(Resources.CopyHandlerSuccessfullyCopy);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}