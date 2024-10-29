using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Connectors.Abstractions.Extensions;

namespace FlowSynx.Core.Features.Exist.Query;

internal class ExistHandler : IRequestHandler<ExistRequest, Result<object>>
{
    private readonly ILogger<ExistHandler> _logger;
    private readonly IContextParser _contextParser;

    public ExistHandler(ILogger<ExistHandler> logger, IContextParser contextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _contextParser = contextParser;
    }

    public async Task<Result<object>> Handle(ExistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _contextParser.Parse(request.Connector);
            var options = request.Options.ToConnectorOptions();
            var response = await contex.Connector.ExistAsync(contex.Context, options, cancellationToken);
            return await Result<object>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<object>.FailAsync(new List<string> { ex.Message });
        }
    }
}