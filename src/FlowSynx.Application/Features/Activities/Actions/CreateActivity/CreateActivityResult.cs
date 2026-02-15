namespace FlowSynx.Application.Features.Activities.Actions.CreateActivity;

public class CreateActivityResult
{
    public string? Status {get; set; }
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Namespace { get; set; }
}