using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Manager;
using FlowSynx.Data.Filter;

namespace FlowSynx.Core.Features.Connectors.Query.List;

internal class ConnectorListHandler : IRequestHandler<ConnectorListRequest, Result<IEnumerable<object>>>
{
    private readonly ILogger<ConnectorListHandler> _logger;
    private readonly IConnectorsManager _connectorsManager;

    public ConnectorListHandler(ILogger<ConnectorListHandler> logger, IConnectorsManager connectorsManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(connectorsManager, nameof(connectorsManager));
        _logger = logger;
        _connectorsManager = connectorsManager;
    }

    public async Task<Result<IEnumerable<object>>> Handle(ConnectorListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var listOptions = new ConnectorListOptions()
            {
                Fields = request.Fields ?? Array.Empty<string>(),
                Filter = request.Filter ?? string.Empty,
                CaseSensitive = request.CaseSensitive ?? false,
                Sort = request.Sort ?? Array.Empty<Sort>(),
                Limit = request.Limit ?? string.Empty
            };

            var response = _connectorsManager.List(listOptions);
            return await Result<IEnumerable<object>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<object>>.FailAsync(new List<string> { ex.Message });
        }
    }
}