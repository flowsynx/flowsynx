namespace FlowSynx.Application.Features.GeneBlueprints.GeneBlueprintRegister;

public class GeneRegisterResult
{
    public string? Status {get; set; }
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Namespace { get; set; }
}