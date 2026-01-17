namespace FlowSynx.Application.Features.Chromosome.Actions.CreateChromosome;

public class CreateChromosomeResult
{
    public string? Status { get; set; }
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Namespace { get; set; }
}