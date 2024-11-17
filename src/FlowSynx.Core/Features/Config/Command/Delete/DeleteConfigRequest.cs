using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Data.DataTableQuery.Filters;
using FlowSynx.Data.DataTableQuery.Sorting;
using FlowSynx.Data.DataTableQuery.Pagination;

namespace FlowSynx.Core.Features.Config.Command.Delete;

public class DeleteConfigRequest : IRequest<Result<IEnumerable<DeleteConfigResponse>>>
{
    public FiltersList? Filters { get; set; }
    public SortsList? Sorts { get; set; }
    public Paging? Paging { get; set; }
    public bool? CaseSensitive { get; set; } = false;
}