using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Workflows.Command.GenerateFromIntent;

public class GenerateFromIntentRequest : IRequest<Result<GenerateFromIntentResponse>>
{
    public required string Goal { get; init; }
    public required string Capabilities { get; init; }
    public bool AutoCreateWorkflow { get; init; } = false;
    public string? OverrideName { get; init; }
    public string? SchemaUrl { get; init; }
}