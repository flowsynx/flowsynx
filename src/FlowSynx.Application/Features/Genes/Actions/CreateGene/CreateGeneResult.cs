namespace FlowSynx.Application.Features.Genes.Actions.CreateGene;

public class CreateGeneResult
{
    public string? Status {get; set; }
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Namespace { get; set; }
}