using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Logging.Extensions;
using FlowSynx.Logging.InMemory;
using FlowSynx.Logging.Options;
using FlowSynx.Logging;

namespace FlowSynx.Core.Features.Logs.Query.List;

internal class LogsListHandler : IRequestHandler<LogsListRequest, Result<IEnumerable<object>>>
{
    private readonly ILogger<LogsListHandler> _logger;
    private readonly ILogManager _logManager;

    public LogsListHandler(ILogger<LogsListHandler> logger, ILogManager logManager, IServiceProvider serviceProvider)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(logManager, nameof(logManager));
        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _logger = logger;
        _logManager = logManager;
    }

    public async Task<Result<IEnumerable<object>>> Handle(LogsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var listOptions = new LogListOptions()
            {
                Fields = request.Fields,
                Filter = request.Filter,
                CaseSensitive = request.CaseSensitive,
                Sort = request.Sort,
                Limit = request.Limit
            };

            var response = _logManager.List(listOptions);
            return await Result<IEnumerable<object>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<object>>.FailAsync(new List<string> { ex.Message });
        }
    }
}