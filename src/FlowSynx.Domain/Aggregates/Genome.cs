using FlowSynx.Domain.Entities;
using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.Aggregates;

public class Genome : AuditableEntity<GenomeId>, IAggregateRoot, ITenantScoped, IUserScoped
{
    public string UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; private set; }
    public List<Chromosome> Chromosomes { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }
    public Dictionary<string, object> SharedEnvironment { get; private set; }
    public Tenant? Tenant { get; set; }

    private Genome() { }

    public Genome(
        GenomeId id,
        string name)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Chromosomes = new List<Chromosome>();
        Metadata = new Dictionary<string, object>();
        SharedEnvironment = new Dictionary<string, object>();
    }

    public void AddChromosome(Chromosome chromosome)
    {
        if (chromosome == null)
            throw new ArgumentNullException(nameof(chromosome));

        if (Chromosomes.Any(c => c.Id == chromosome.Id))
            throw new DomainException($"Chromosome with ID {chromosome.Id} already exists");

        Chromosomes.Add(chromosome);
    }

    public void RemoveChromosome(ChromosomeId chromosomeId)
    {
        var chromosome = Chromosomes.FirstOrDefault(c => c.Id == chromosomeId);
        if (chromosome == null)
            throw new DomainException($"Chromosome {chromosomeId} not found");

        Chromosomes.Remove(chromosome);
    }

    public Chromosome GetChromosome(ChromosomeId chromosomeId) =>
        Chromosomes.FirstOrDefault(c => c.Id == chromosomeId);

    public GeneInstance GetGene(GeneInstanceId instanceId)
    {
        foreach (var chromosome in Chromosomes)
        {
            var gene = chromosome.GetGene(instanceId);
            if (gene != null) return gene;
        }
        return null;
    }
}