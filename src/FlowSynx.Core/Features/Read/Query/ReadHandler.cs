using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Abstractions.Extensions;

namespace FlowSynx.Core.Features.Read.Query;

internal class ReadHandler : IRequestHandler<ReadRequest, Result<ReadResult>>
{
    private readonly ILogger<ReadHandler> _logger;
    private readonly IContextParser _contextParser;

    public ReadHandler(ILogger<ReadHandler> logger, IContextParser connectorContextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _contextParser = connectorContextParser;
    }

    public async Task<Result<ReadResult>> Handle(ReadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _contextParser.Parse(request.Connector);
            var options = request.Options.ToConnectorOptions();
            var response = await contex.Connector.ReadAsync(contex.Context, options, cancellationToken);
            return await Result<ReadResult>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<ReadResult>.FailAsync(new List<string> { ex.Message });
        }
    }
}