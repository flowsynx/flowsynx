using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Workflows.Command.GenerateFromIntent;

public class GenerateFromIntentRequest : IRequest<Result<GenerateFromIntentResponse>>
{
    public required string Goal { get; init; }
    public string? CapabilitiesJson { get; init; }
    public bool AutoCreate { get; init; } = false;
    public string? NameOverride { get; init; }
    public string? SchemaUrl { get; init; }
}