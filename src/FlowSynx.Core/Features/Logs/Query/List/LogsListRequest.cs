using FlowSynx.Abstractions;
using MediatR;

namespace FlowSynx.Core.Features.Logs.Query.List;

public class LogsListRequest : IRequest<Result<IEnumerable<LogsListResponse>>>
{
    public string? MinAge { get; set; }
    public string? MaxAge { get; set; }
    public string? Level { get; set; }
}

