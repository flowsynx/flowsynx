using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Connectors.Abstractions.Extensions;

namespace FlowSynx.Core.Features.List.Query;

internal class ListHandler : IRequestHandler<ListRequest, Result<IEnumerable<object>>>
{
    private readonly ILogger<ListHandler> _logger;
    private readonly IContextParser _contextParser;

    public ListHandler(ILogger<ListHandler> logger, IContextParser contextParser)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _contextParser = contextParser;
    }

    public async Task<Result<IEnumerable<object>>> Handle(ListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contex = _contextParser.Parse(request.Entity);
            var options = request.Options.ToConnectorOptions();
            var response = await contex.CurrentConnector.ListAsync(contex.Entity, contex.NextConnector, options, cancellationToken);
            return await Result<IEnumerable<object>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<object>>.FailAsync(new List<string> { ex.Message });
        }
    }
}