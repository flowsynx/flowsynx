using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Logs.Query.LogsList;

public class LogsListRequest : IRequest<Result<IEnumerable<LogsListResponse>>>
{
    public string? Level { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Message { get; set; }
}