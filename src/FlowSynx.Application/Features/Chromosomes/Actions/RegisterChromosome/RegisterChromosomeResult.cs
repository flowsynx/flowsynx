namespace FlowSynx.Application.Features.Chromosomes.Actions.RegisterChromosome;

public class RegisterChromosomeResult
{
    public string? Status {get; set; }
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Namespace { get; set; }
}