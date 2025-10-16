namespace FlowSynx.Application.Features.Workflows.Query.WorkflowsList;

public class WorkflowListResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string? SchemaUrl { get; set; }
}
