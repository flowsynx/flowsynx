using FlowSynx.Domain.DomainEvents;
using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.Entities;

public class Chromosome : AuditableEntity<ChromosomeId>, ITenantScoped, IUserScoped
{
    public string UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; private set; }
    public List<GeneInstance> Genes { get; private set; }
    public CellularEnvironment CellularEnvironment { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }
    public List<GeneExecutionResult> ExecutionResults { get; private set; }
    public Tenant? Tenant { get; set; }

    private Chromosome() { }

    public Chromosome(
        ChromosomeId id,
        string name,
        CellularEnvironment cellularEnvironment = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CellularEnvironment = cellularEnvironment ?? new CellularEnvironment(
            new ImmuneResponse(),
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
            otherGene.RemoveDependency(instanceId);
        }

        Genes.Remove(gene);
    }

    public void UpdateCellularEnvironment(CellularEnvironment environment)
    {
        CellularEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public void AddExecutionResults(List<GeneExecutionResult> results)
    {
        ExecutionResults.Clear();
        ExecutionResults.AddRange(results);
        AddDomainEvent(new ChromosomeExecuted(this, results));
    }

    public GeneInstance GetGene(GeneInstanceId instanceId) =>
        Genes.FirstOrDefault(g => g.Id == instanceId);
}
