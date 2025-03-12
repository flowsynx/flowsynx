namespace FlowSynx.Application.Features.Workflows.Query.List;

public class WorkflowListResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTime ModifiedDate { get; set; }
}