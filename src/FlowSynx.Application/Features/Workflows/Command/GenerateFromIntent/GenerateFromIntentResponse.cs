namespace FlowSynx.Application.Features.Workflows.Command.GenerateFromIntent;

public class GenerateFromIntentResponse
{
    public Guid? WorkflowId { get; init; }
    public string? Name { get; init; }
    public required string WorkflowJson { get; init; }
    public required string Plan { get; init; }
    public string? SchemaUrl { get; init; }
}