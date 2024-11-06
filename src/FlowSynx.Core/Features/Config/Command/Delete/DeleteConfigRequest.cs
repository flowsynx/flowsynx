using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Data.Filter;

namespace FlowSynx.Core.Features.Config.Command.Delete;

public class DeleteConfigRequest : IRequest<Result<IEnumerable<DeleteConfigResponse>>>
{
    public string[]? Fields { get; set; }
    public string? Filter { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public Sort[]? Sort { get; set; }
    public string? Limit { get; set; }
}