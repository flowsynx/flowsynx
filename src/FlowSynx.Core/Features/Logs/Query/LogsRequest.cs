using FlowSynx.Abstractions;
using MediatR;

namespace FlowSynx.Core.Features.Logs.Query;

public class LogsRequest : IRequest<Result<IEnumerable<LogsResponse>>>
{
    public string? MinAge { get; set; }
    public string? MaxAge { get; set; }
    public string? Level { get; set; }
}

