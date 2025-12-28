using FlowSynx.Domain.Primitives;
using MediatR;

namespace FlowSynx.Application.Features.LogEntries.Query.LogEntriesList;

public class LogEntriesListRequestTdo
{
    public string? Level { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Message { get; set; }
}

public class LogEntriesListRequest : IRequest<PaginatedResult<LogEntriesListResponse>>
{
    public string? Level { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Message { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}