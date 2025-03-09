namespace FlowSynx.Core.Features.Workflows.Query.Details;

public class WorkflowDetailsResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Template { get; set; }
}