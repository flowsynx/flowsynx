using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Config.Command.Delete;

public class DeleteConfigRequest : IRequest<Result<IEnumerable<DeleteConfigResponse>>>
{
    public string[]? Fields { get; set; }
    public string? Filter { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public string? Sort { get; set; }
    public string? Limit { get; set; }
}