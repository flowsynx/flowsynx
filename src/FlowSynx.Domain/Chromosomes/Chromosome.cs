using FlowSynx.Domain.Chromosomes.Events;
using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.Chromosomes;

public class Chromosome : AuditableEntity<ChromosomeId>, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; }
    public string Name { get; private set; }
    public List<GeneInstance> Genes { get; private set; }
    public EnvironmentalFactor EnvironmentalFactor { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }
    public List<GeneExecutionResult> ExecutionResults { get; private set; }
    public Tenant? Tenant { get; set; }

    private Chromosome() { }

    public Chromosome(
        TenantId tenantId,
        string userId,
        ChromosomeId id,
        string name,
        EnvironmentalFactor environmentalFactor = null)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        EnvironmentalFactor = environmentalFactor ?? new EnvironmentalFactor(
            new DefenseMechanism(),
            new ResourceConstraints(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>());
        Genes = new List<GeneInstance>();
        Metadata = new Dictionary<string, object>();
        ExecutionResults = new List<GeneExecutionResult>();
    }

    public GeneInstance AddGene(
        GeneInstanceId instanceId,
        GeneBlueprintId blueprintId,
        Dictionary<string, object> parameters = null,
        ExpressionConfiguration expressionConfiguration = null)
    {
        if (Genes.Any(g => g.Id == instanceId))
            throw new DomainException($"Gene instance with ID {instanceId} already exists");

        var geneInstance = new GeneInstance(
            TenantId,
            UserId,
            instanceId,
            blueprintId,
            parameters,
            expressionConfiguration);

        Genes.Add(geneInstance);
        return geneInstance;
    }

    public void RemoveGene(GeneInstanceId instanceId)
    {
        var gene = Genes.FirstOrDefault(g => g.Id == instanceId);
        if (gene == null)
            throw new DomainException($"Gene instance {instanceId} not found");

        // Remove dependencies on this gene
        foreach (var otherGene in Genes)
        {
            otherGene.RemoveRegulatoryNetwork(instanceId);
        }

        Genes.Remove(gene);
    }

    public void UpdateEnvironmentalFactor(EnvironmentalFactor environment)
    {
        EnvironmentalFactor = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public void AddExecutionResults(List<GeneExecutionResult> results)
    {
        ExecutionResults.Clear();
        ExecutionResults.AddRange(results);
        AddDomainEvent(new ChromosomeExecuted(this, results));
    }

    public GeneInstance? GetGene(GeneInstanceId instanceId) =>
        Genes.FirstOrDefault(g => g.Id == instanceId);
}
