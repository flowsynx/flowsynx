namespace FlowSynx.Application.Features.Genes.Actions.RegisterGene;

public class RegisterGeneResult
{
    public string? Status {get; set; }
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Namespace { get; set; }
}