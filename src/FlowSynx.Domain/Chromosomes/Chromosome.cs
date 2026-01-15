using FlowSynx.Domain.Genes;
using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.Chromosomes;

public class Chromosome : AuditableEntity<Guid>, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Description { get; set; }
    public ChromosomeSpec Spec { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
    public Guid? GenomeId { get; set; }

    // Navigation properties
    public ICollection<GeneInstances.GeneInstance> Genes { get; set; } = new List<GeneInstances.GeneInstance>();
    public Genome? Genome { get; set; }


    //public TenantId TenantId { get; set; }
    //public string UserId { get; set; }
    //public string Namespace { get; set; }
    //public string Name { get; set; }
    //public string Annotation { get; set; }
    //public List<GeneInstance> Genes { get; set; }
    //public CellularEnvironment CellularEnvironment { get; set; }
    //public Dictionary<string, object> EpigeneticMarks { get; set; }
    //public List<ExpressionResult> ExpressionResults { get; set; }
    //public Tenant? Tenant { get; set; }

    //public Chromosome() { }

    ////public Chromosome(
    ////    TenantId tenantId,
    ////    string userId,
    ////    ChromosomeId id,
    ////    string name,
    ////    CellularEnvironment cellularEnvironment = null)
    ////{
    ////    TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
    ////    UserId = userId ?? throw new ArgumentNullException(nameof(userId));
    ////    Id = id ?? throw new ArgumentNullException(nameof(id));
    ////    Name = name ?? throw new ArgumentNullException(nameof(name));
    ////    CellularEnvironment = cellularEnvironment ?? new CellularEnvironment(
    ////        new ImmuneSystem(),
    ////        new NutrientConstraints(),
    ////        new Dictionary<string, object>(),
    ////        new Dictionary<string, object>());
    ////    Genes = new List<GeneInstance>();
    ////    EpigeneticMarks = new Dictionary<string, object>();
    ////    ExpressionResults = new List<ExpressionResult>();
    ////}

    //public GeneInstance AddGene(
    //    GeneInstanceId instanceId,
    //    GeneBlueprintId blueprintId,
    //    Dictionary<string, object> parameters = null,
    //    ExpressionProfile expressionProfile = null)
    //{
    //    if (Genes.Any(g => g.Id == instanceId))
    //        throw new GeneInstanceExistsException(instanceId);

    //    var geneInstance = new GeneInstance(
    //        TenantId,
    //        UserId,
    //        instanceId,
    //        blueprintId,
    //        parameters,
    //        expressionProfile);

    //    Genes.Add(geneInstance);
    //    return geneInstance;
    //}

    //public void RemoveGene(GeneInstanceId instanceId)
    //{
    //    var gene = Genes.FirstOrDefault(g => g.Id == instanceId);
    //    if (gene == null)
    //        throw new GeneInstanceNotFoundException(instanceId);

    //    // Remove dependencies on this gene
    //    foreach (var otherGene in Genes)
    //    {
    //        otherGene.RemoveRegulatoryNetwork(instanceId);
    //    }

    //    Genes.Remove(gene);
    //}

    //public void UpdateCellularEnvironment(CellularEnvironment environment)
    //{
    //    CellularEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));
    //}

    //public void AddExpressionResults(List<ExpressionResult> results)
    //{
    //    ExpressionResults.Clear();
    //    ExpressionResults.AddRange(results);
    //    AddDomainEvent(new ChromosomeExpressed(this, results));
    //}

    //public GeneInstance? GetGene(GeneInstanceId instanceId) =>
    //    Genes.FirstOrDefault(g => g.Id == instanceId);
}

public class ChromosomeSpec
{
    public string Description { get; set; }

    public List<GeneInstance> Genes { get; set; } = new List<GeneInstance>();

    public CellularEnvironment Environment { get; set; }

    public ChromosomeValidation Validation { get; set; }

    public OutputSpec Output { get; set; }
}

public class GeneInstance
{
    public string Id { get; set; }

    public GeneReference GeneRef { get; set; }

    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    public GeneConfigJson Config { get; set; }

    public List<string> Dependencies { get; set; } = new List<string>();

    public string When { get; set; } // Condition for execution

    public RetryPolicy Retry { get; set; }

    public int Timeout { get; set; } = 5000;
}

public class GeneReference
{
    public string Name { get; set; }

    public string Version { get; set; } = "latest";

    public string Namespace { get; set; } = "default";
}

public class GeneConfigJson
{
    public string Operation { get; set; }

    public string Mode { get; set; } = "default";

    public bool Parallel { get; set; } = false;

    public int Priority { get; set; } = 1;
}

public class CellularEnvironment
{
    public ImmuneResponse ErrorHandling { get; set; }

    public ResourceConstraints Resources { get; set; }

    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

    public Dictionary<string, object> Shared { get; set; } = new Dictionary<string, object>();

    public SecurityContext Security { get; set; }
}

public class ResourceConstraints
{
    public string Cpu { get; set; } = "100m";

    public string Memory { get; set; } = "128Mi";

    public string Storage { get; set; } = "1Gi";

    public int MaxParallel { get; set; } = 5;
}

public class SecurityContext
{
    public int? RunAsUser { get; set; }

    public int? RunAsGroup { get; set; }

    public List<string> Capabilities { get; set; } = new List<string>();

    public bool ReadOnlyRootFilesystem { get; set; } = false;
}

public class ChromosomeValidation
{
    public string Schema { get; set; }

    public List<ValidationRule> Rules { get; set; } = new List<ValidationRule>();
}

public class OutputSpec
{
    public string Format { get; set; } = "json";

    public string Path { get; set; }

    public List<OutputVariable> Variables { get; set; } = new List<OutputVariable>();
}

public class OutputVariable
{
    public string Name { get; set; }

    public string From { get; set; } // geneId.result.field

    public string Transform { get; set; } // jsonpath, jq, template
}