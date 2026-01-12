using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.GeneBlueprints;

public class GeneBlueprint : AuditableEntity<Guid>, IAggregateRoot, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public GeneBlueprintSpec Spec { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
    public string Owner { get; set; }
    public string Status { get; set; } = "active";
    public bool IsShared { get; set; }
    public Tenant? Tenant { get; set; }

    // Navigation properties
    //public ICollection<Chromosome> Chromosomes { get; set; } = new List<Chromosome>();




    //public string Namespace { get; set; }
    //public string Generation { get; set; }
    //public string Phenotypic { get; set; }
    //public string Annotation { get; set; }
    //public List<NucleotideSequence> NucleotideSequences { get; set; }   // Parameters
    //public ExpressionProfile ExpressionProfile { get; set; }
    //public EpistaticInteraction EpistaticInteraction { get; set; }      // Compatibility Matrix
    //public ImmuneSystem ImmuneSystem { get; set; }                      // Error Handling
    //public ExpressedProtein ExpressedProtein { get; set; }              // ExecutableComponent
    //public Dictionary<string, object> EpigeneticMarks { get; set; }     // Metadata
    //public Tenant? Tenant { get; set; }

    // Domain invariants
    //public GeneBlueprint() { } // For EF Core

    //public GeneBlueprint(
    //    TenantId tenantId,
    //    string userId,
    //    GeneBlueprintId id,
    //    string @namespace,
    //    string generation,
    //    string phenotypic,
    //    string annotation,
    //    List<NucleotideSequence> nucleotideSequences,
    //    ExpressionProfile expressionProfile,
    //    EpistaticInteraction epistaticInteraction,
    //    ImmuneSystem immuneSystem,
    //    ExpressedProtein expressedProtein)
    //{
    //    TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
    //    UserId = userId ?? throw new ArgumentNullException(nameof(userId));
    //    Id = id ?? throw new ArgumentNullException(nameof(id));
    //    Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
    //    Generation = generation ?? throw new ArgumentNullException(nameof(generation));
    //    Phenotypic = phenotypic ?? throw new ArgumentNullException(nameof(phenotypic));
    //    Annotation = annotation ?? throw new ArgumentNullException(nameof(annotation));
    //    NucleotideSequences = nucleotideSequences ?? new List<NucleotideSequence>();
    //    ExpressionProfile = expressionProfile ?? throw new ArgumentNullException(nameof(expressionProfile));
    //    EpistaticInteraction = epistaticInteraction ?? throw new ArgumentNullException(nameof(epistaticInteraction));
    //    ImmuneSystem = immuneSystem ?? throw new ArgumentNullException(nameof(immuneSystem));
    //    ExpressedProtein = expressedProtein ?? throw new ArgumentNullException(nameof(expressedProtein));
    //    EpigeneticMarks = new Dictionary<string, object>();

    //    ValidateState();
    //    AddDomainEvent(new GeneBlueprintCreated(this));
    //}

    //public void Update(
    //    string? phenotypic = null,
    //    string? annotation = null,
    //    ExpressionProfile? expressionProfile = null,
    //    ImmuneSystem? immuneSystem = null,
    //    Dictionary<string, object>? epigeneticMarks = null)
    //{
    //    if (phenotypic != null) Phenotypic = phenotypic;
    //    if (annotation != null) Annotation = annotation;
    //    if (expressionProfile != null) ExpressionProfile = expressionProfile;
    //    if (immuneSystem != null) ImmuneSystem = immuneSystem;
    //    if (epigeneticMarks != null) EpigeneticMarks = epigeneticMarks;

    //    ValidateState();
    //    AddDomainEvent(new GeneBlueprintUpdated(this));
    //}

    //private void ValidateState()
    //{
    //    if (string.IsNullOrWhiteSpace(Generation))
    //        throw new GeneBlueprintGenerationRequiredException();

    //    if (string.IsNullOrWhiteSpace(Phenotypic))
    //        throw new GeneBlueprintPhenotypicRequiredException();

    //    if (string.IsNullOrWhiteSpace(Annotation))
    //        throw new GeneBlueprintAnnotationRequiredException();

    //    if (ExpressedProtein == null)
    //        throw new GeneBlueprintExpressedProteinRequiredException();
    //}

    //public bool HasImmuneSystem() => ImmuneSystem != null;
}

public class GeneBlueprintSpec
{
    public string Description { get; set; }
    public string GeneticBlueprint { get; set; }
    public List<ParameterDefinition> Parameters { get; set; } = new List<ParameterDefinition>();
    public ExpressionProfile ExpressionProfile { get; set; }
    public CompatibilityMatrix Compatibility { get; set; }
    public ImmuneResponse ImmuneResponse { get; set; }
    public ExecutableComponent Executable { get; set; }
    public List<ValidationRule> ValidationRules { get; set; } = new List<ValidationRule>();
    public List<string> Tags { get; set; } = new List<string>();
}

public class ParameterDefinition
{
    public string Name { get; set; }
    public string Type { get; set; } = "string";
    public string Description { get; set; }
    public object Default { get; set; }
    public bool Required { get; set; } = false;
    public object Schema { get; set; }
    public List<string> Validation { get; set; } = new List<string>();
}

public class ExpressionProfile
{
    public string DefaultOperation { get; set; }
    public List<ExpressionCondition> Conditions { get; set; } = new List<ExpressionCondition>();
    public int Priority { get; set; } = 1;
    public string ExecutionMode { get; set; } = "synchronous";
    public int Timeout { get; set; } = 5000;
    public RetryPolicy RetryPolicy { get; set; }
}

public class ExpressionCondition
{
    public string When { get; set; }
    public string Field { get; set; }
    public string Operator { get; set; } = "equals";
    public object Value { get; set; }
    public string Action { get; set; } = "skip"; // "skip", "execute", "fail"
}

public class RetryPolicy
{
    public int MaxAttempts { get; set; } = 3;
    public int Delay { get; set; } = 1000;
    public float BackoffMultiplier { get; set; } = 1.5f;
    public int MaxDelay { get; set; } = 10000;
}

public class CompatibilityMatrix
{
    public string MinRuntimeVersion { get; set; }
    public List<string> Platforms { get; set; } = new List<string>();
    public List<Dependency> Dependencies { get; set; } = new List<Dependency>();
    public List<string> IncompatibleWith { get; set; } = new List<string>();
    public Dictionary<string, object> Constraints { get; set; } = new Dictionary<string, object>();
}

public class Dependency
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string Type { get; set; } = "gene"; // "gene", "library", "service"
}

public class ImmuneResponse
{
    public string ErrorHandling { get; set; } = "propagate";
    public int MaxRetries { get; set; } = 3;
    public int RetryDelay { get; set; } = 1000;
    public Fallback Fallback { get; set; }
    public CircuitBreaker CircuitBreaker { get; set; }
    public HealthCheck HealthCheck { get; set; }
}

public class Fallback
{
    public string Operation { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public object DefaultValue { get; set; }
}

public class CircuitBreaker
{
    public int FailureThreshold { get; set; } = 5;
    public int SuccessThreshold { get; set; } = 2;
    public int Timeout { get; set; } = 30000;
    public int HalfOpenMaxCalls { get; set; } = 3;
}

public class HealthCheck
{
    public string Endpoint { get; set; }
    public int Interval { get; set; } = 30000;
    public int Timeout { get; set; } = 5000;
}

public class ExecutableComponent
{
    public string Type { get; set; } = "script"; // "assembly", "script", "container", "http", "grpc"
    public string Language { get; set; } = "javascript"; // "javascript", "python", "csharp", "powershell"
    public string Source { get; set; }
    public string EntryPoint { get; set; }
    public string Assembly { get; set; }
    public ContainerSpec Container { get; set; }
    public HttpEndpoint Http { get; set; }
    public GrpcEndpoint Grpc { get; set; }
    public Dictionary<string, object> Config { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();
}

public class ContainerSpec
{
    public string Image { get; set; }
    public List<string> Command { get; set; } = new List<string>();
    public List<string> Args { get; set; } = new List<string>();
    public Dictionary<string, string> Env { get; set; } = new Dictionary<string, string>();
    public ResourceRequirements Resources { get; set; }
    public List<ContainerPort> Ports { get; set; } = new List<ContainerPort>();
}

public class ResourceRequirements
{
    public Dictionary<string, string> Requests { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Limits { get; set; } = new Dictionary<string, string>();
}

public class ContainerPort
{
    public int Port { get; set; }
    public string Protocol { get; set; } = "TCP";
}

public class HttpEndpoint
{
    public string Url { get; set; }
    public string Method { get; set; } = "POST";
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public int Timeout { get; set; } = 30000;
    public bool Retry { get; set; } = true;
}

public class GrpcEndpoint
{
    public string Service { get; set; }
    public string Method { get; set; }
    public string Address { get; set; }
    public int Timeout { get; set; } = 30000;
}

public class ValidationRule
{
    public string Field { get; set; }
    public string Rule { get; set; } // "required", "regex", "min", "max", "custom"
    public object Value { get; set; }
    public string Message { get; set; }
}