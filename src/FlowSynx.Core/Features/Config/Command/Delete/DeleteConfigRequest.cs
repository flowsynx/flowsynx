using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Data;

namespace FlowSynx.Core.Features.Config.Command.Delete;

public class DeleteConfigRequest : IRequest<Result<IEnumerable<DeleteConfigResponse>>>
{
    public FilterList? Filter { get; set; }
    public SortList? Sort { get; set; }
    public Paging? Paging { get; set; }
    public bool? CaseSensitive { get; set; } = false;
}