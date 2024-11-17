using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Data.DataTableQuery.Fields;
using FlowSynx.Data.DataTableQuery.Filters;
using FlowSynx.Data.DataTableQuery.Sorting;
using FlowSynx.Data.DataTableQuery.Pagination;

namespace FlowSynx.Core.Features.Connectors.Query.List;

public class ConnectorListRequest : IRequest<Result<IEnumerable<object>>>
{
    public FieldsList? Fields { get; set; }
    public FiltersList? Filters { get; set; }
    public SortsList? Sorts { get; set; }
    public Paging? Paging { get; set; }
    public bool? CaseSensitive { get; set; } = false;
}