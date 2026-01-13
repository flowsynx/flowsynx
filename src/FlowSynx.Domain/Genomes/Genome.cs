using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.Genomes;

public class Genome : AuditableEntity<Guid>, IAggregateRoot, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Description { get; set; }
    public GenomeSpec Spec { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, object> SharedEnvironment { get; set; } = new Dictionary<string, object>();
    public string Owner { get; set; }
    public bool IsShared { get; set; }

    // Navigation properties
    public ICollection<Chromosome> Chromosomes { get; set; } = new List<Chromosome>();
    public ICollection<ExecutionRecord> Executions { get; set; } = new List<ExecutionRecord>();

    //public TenantId TenantId { get; set; }
    //public string UserId { get; set; }
    //public string Namespace { get; private set; }
    //public string SpeciesName { get; private set; }
    //public Dictionary<string, object> EpigeneticMarks { get; private set; }
    //public Dictionary<string, object> CytoplasmicEnvironment { get; private set; }
    //public List<Chromosome> Chromosomes { get; private set; }
    //public ICollection<ExecutionRecord> Executions { get; private set; }
    //public Tenant? Tenant { get; set; }

    //private Genome() { }

    //public Genome(
    //    TenantId tenantId,
    //    string userId,
    //    GenomeId id,
    //    string speciesName)
    //{
    //    TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
    //    UserId = userId ?? throw new ArgumentNullException(nameof(userId));
    //    Id = id ?? throw new ArgumentNullException(nameof(id));
    //    SpeciesName = speciesName ?? throw new ArgumentNullException(nameof(speciesName));
    //    Chromosomes = new List<Chromosome>();
    //    EpigeneticMarks = new Dictionary<string, object>();
    //    CytoplasmicEnvironment = new Dictionary<string, object>();
    //}

    //public void AddChromosome(Chromosome chromosome)
    //{
    //    if (chromosome == null)
    //        throw new ArgumentNullException(nameof(chromosome));

    //    if (Chromosomes.Any(c => c.Id == chromosome.Id))
    //        throw new ChromosomeIdExistsException(chromosome.Id);

    //    Chromosomes.Add(chromosome);
    //}

    //public void RemoveChromosome(ChromosomeId chromosomeId)
    //{
    //    var chromosome = Chromosomes.FirstOrDefault(c => c.Id == chromosomeId);
    //    if (chromosome == null)
    //        throw new ChromosomeIdNotFoundException(chromosomeId);

    //    Chromosomes.Remove(chromosome);
    //}

    //public Chromosome? GetChromosome(ChromosomeId chromosomeId) =>
    //    Chromosomes.FirstOrDefault(c => c.Id == chromosomeId);

    //public GeneInstance? GetGene(GeneInstanceId instanceId)
    //{
    //    foreach (var chromosome in Chromosomes)
    //    {
    //        var gene = chromosome.GetGene(instanceId);
    //        if (gene != null) return gene;
    //    }
    //    return null;
    //}
}

public class GenomeSpec
{
    public string Description { get; set; }

    public List<ChromosomeReference> Chromosomes { get; set; } = new List<ChromosomeReference>();

    public GenomeEnvironment Environment { get; set; }

    public GenomeValidation Validation { get; set; }

    public ExecutionPlan Execution { get; set; }

    public GenomeOutput Output { get; set; }
}

public class ChromosomeReference
{
    public string Ref { get; set; }

    public string Name { get; set; }

    public string Namespace { get; set; } = "default";

    public string When { get; set; }

    public bool Parallel { get; set; } = false;

    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}

public class GenomeEnvironment
{
    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

    public List<SecretReference> Secrets { get; set; } = new List<SecretReference>();

    public List<ConfigMapReference> ConfigMaps { get; set; } = new List<ConfigMapReference>();

    public Dictionary<string, object> Shared { get; set; } = new Dictionary<string, object>();
}

public class SecretReference
{
    public string Name { get; set; }

    public string Namespace { get; set; } = "default";

    public List<string> Keys { get; set; } = new List<string>();
}

public class ConfigMapReference
{
    public string Name { get; set; }

    public string Namespace { get; set; } = "default";

    public List<string> Keys { get; set; } = new List<string>();
}

public class GenomeValidation
{
    public string Schema { get; set; }

    public List<ValidationRule> Rules { get; set; } = new List<ValidationRule>();
}

public class ExecutionPlan
{
    public string Strategy { get; set; } = "sequential"; // "sequential", "parallel", "dependency"

    public int MaxParallel { get; set; } = 3;

    public int Timeout { get; set; } = 300000; // 5 minutes

    public RetryPolicy Retry { get; set; }
}

public class GenomeOutput
{
    public string Format { get; set; } = "json";

    public string Path { get; set; }

    public List<ArtifactSpec> Artifacts { get; set; } = new List<ArtifactSpec>();
}

public class ArtifactSpec
{
    public string Name { get; set; }

    public string Type { get; set; } // "file", "data", "report"

    public string Path { get; set; }

    public string Content { get; set; }
}















public class ExecutionRecord : AuditableEntity<Guid>, IAggregateRoot
{
    public string ExecutionId { get; set; }
    public string TargetType { get; set; } // "gene", "chromosome", "genome"
    public Guid TargetId { get; set; }
    public string TargetName { get; set; }
    public string Namespace { get; set; }
    public Dictionary<string, object> Request { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Response { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public string Status { get; set; } // "pending", "running", "completed", "failed", "cancelled"
    public int Progress { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public long Duration { get; set; }
    public string TriggeredBy { get; set; }

    // Navigation properties
    public ICollection<ExecutionLog> Logs { get; set; } = new List<ExecutionLog>();
    public ICollection<ExecutionArtifact> Artifacts { get; set; } = new List<ExecutionArtifact>();
}

public class ExecutionLog : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid ExecutionRecordId { get; set; }
    public string Level { get; set; } // "info", "warn", "error", "debug"
    public string Message { get; set; }
    public string Source { get; set; }
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ExecutionRecord ExecutionRecord { get; set; }
}

public class ExecutionArtifact : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid ExecutionRecordId { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // "file", "data", "report"
    public object Content { get; set; }
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ExecutionRecord ExecutionRecord { get; set; }
}