using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Logs.Query.LogsList;

public class LogsListRequestTdo
{
    public string? Level { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Message { get; set; }
}

public class LogsListRequest : IRequest<PaginatedResult<LogsListResponse>>
{
    public string? Level { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Message { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}