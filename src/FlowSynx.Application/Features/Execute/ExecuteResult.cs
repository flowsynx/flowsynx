namespace FlowSynx.Application.Features.Execute;

public class ExecuteResult
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ExecutionId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}