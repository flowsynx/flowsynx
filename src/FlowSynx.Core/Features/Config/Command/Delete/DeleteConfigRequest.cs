using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Config.Command.Delete;

public class DeleteConfigRequest : IRequest<Result<IEnumerable<DeleteConfigResponse>>>
{
    public string? Include { get; set; }
    public string? Exclude { get; set; }
    public string? MinimumAge { get; set; }
    public string? MaximumAge { get; set; }
    public bool CaseSensitive { get; set; } = false;
}