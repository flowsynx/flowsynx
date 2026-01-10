using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.Genomes;

public class Genome : AuditableEntity<GenomeId>, IAggregateRoot, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; }
    public string SpeciesName { get; private set; }
    public List<Chromosome> Chromosomes { get; private set; }
    public Dictionary<string, object> EpigeneticMarks { get; private set; }
    public Dictionary<string, object> CytoplasmicEnvironment { get; private set; }
    public Tenant? Tenant { get; set; }

    private Genome() { }

    public Genome(
        TenantId tenantId,
        string userId,
        GenomeId id,
        string speciesName)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Id = id ?? throw new ArgumentNullException(nameof(id));
        SpeciesName = speciesName ?? throw new ArgumentNullException(nameof(speciesName));
        Chromosomes = new List<Chromosome>();
        EpigeneticMarks = new Dictionary<string, object>();
        CytoplasmicEnvironment = new Dictionary<string, object>();
    }

    public void AddChromosome(Chromosome chromosome)
    {
        if (chromosome == null)
            throw new ArgumentNullException(nameof(chromosome));

        if (Chromosomes.Any(c => c.Id == chromosome.Id))
            throw new ChromosomeIdExistsException(chromosome.Id);

        Chromosomes.Add(chromosome);
    }

    public void RemoveChromosome(ChromosomeId chromosomeId)
    {
        var chromosome = Chromosomes.FirstOrDefault(c => c.Id == chromosomeId);
        if (chromosome == null)
            throw new ChromosomeIdNotFoundException(chromosomeId);

        Chromosomes.Remove(chromosome);
    }

    public Chromosome? GetChromosome(ChromosomeId chromosomeId) =>
        Chromosomes.FirstOrDefault(c => c.Id == chromosomeId);

    public GeneInstance? GetGene(GeneInstanceId instanceId)
    {
        foreach (var chromosome in Chromosomes)
        {
            var gene = chromosome.GetGene(instanceId);
            if (gene != null) return gene;
        }
        return null;
    }
}