using FlowSynx.Abstractions;
using FlowSynx.Data.DataTableQuery.Fields;
using FlowSynx.Data.DataTableQuery.Filters;
using FlowSynx.Data.DataTableQuery.Pagination;
using FlowSynx.Data.DataTableQuery.Sorting;
using MediatR;

namespace FlowSynx.Core.Features.Logs.Query.List;

public class LogsListRequest : IRequest<Result<IEnumerable<object>>>
{
    public FieldsList? Fields { get; set; }
    public FiltersList? Filters { get; set; }
    public SortsList? Sorts { get; set; }
    public Paging? Paging { get; set; }
    public bool? CaseSensitive { get; set; } = false;
}

