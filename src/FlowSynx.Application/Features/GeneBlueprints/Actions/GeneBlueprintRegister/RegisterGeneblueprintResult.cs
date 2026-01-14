namespace FlowSynx.Application.Features.GeneBlueprints.Actions.GeneBlueprintRegister;

public class RegisterGeneblueprintResult
{
    public string? Status {get; set; }
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Namespace { get; set; }
}