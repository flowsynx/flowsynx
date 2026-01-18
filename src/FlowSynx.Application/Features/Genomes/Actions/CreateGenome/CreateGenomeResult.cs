namespace FlowSynx.Application.Features.Genomes.Actions.CreateGenome;

public class CreateGenomeResult
{
    public string? Status { get; set; }
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Namespace { get; set; }
}