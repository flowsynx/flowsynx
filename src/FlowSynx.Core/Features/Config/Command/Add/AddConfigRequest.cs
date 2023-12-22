using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Config.Command.Add;

public class AddConfigRequest : IRequest<Result<AddConfigResponse>>
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public object? Specifications { get; set; }
}