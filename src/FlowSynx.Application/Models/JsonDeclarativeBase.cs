using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genes;
using FlowSynx.Domain.Genomes;

namespace FlowSynx.Application.Models;

public abstract class JsonDeclarativeBase
{
    public string ApiVersion { get; set; } = "genome/v1";
    public string Kind { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public object Spec { get; set; }
}

// Gene JSON
public class GeneJson
{
    public string ApiVersion { get; set; } = "gene/v1";
    public string Kind { get; set; } = "Gene";
    public GeneMetadata Metadata { get; set; }
    public GeneSpecification Specification { get; set; }
}

public class GeneMetadata
{
    public string Name { get; set; }
    public string Namespace { get; set; } = "default";
    public string Id { get; set; }
    public string Version { get; set; } = "1.0.0";
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
    public DateTimeOffset CreatedAt { get; set; }
    public bool Shared { get; set; } = false;
    public string Owner { get; set; }
}

//public class GeneBlueprintSpec
//{
//    public string Description { get; set; }
//    public string GeneticBlueprint { get; set; }

//    public List<ParameterDefinitionJson> Parameters { get; set; } = new List<ParameterDefinitionJson>();

//    public ExpressionProfileJson ExpressionProfile { get; set; }

//    public CompatibilityMatrixJson Compatibility { get; set; }
//    public ImmuneResponseJson ImmuneResponse { get; set; }
//    public ExecutableComponentJson Executable { get; set; }
//    public List<ValidationRuleJson> ValidationRules { get; set; } = new List<ValidationRuleJson>();
//    public List<string> Tags { get; set; } = new List<string>();
//}

//public class ParameterDefinitionJson
//{
//    public string Name { get; set; }

//    public string Type { get; set; } = "string";

//    public string Description { get; set; }

//    public object Default { get; set; }

//    public bool Required { get; set; } = false;

//    public object Schema { get; set; }

//    public List<string> Validation { get; set; } = new List<string>();
//}

//public class ExpressionProfileJson
//{
//    public string DefaultOperation { get; set; }

//    public List<ExpressionConditionJson> Conditions { get; set; } = new List<ExpressionConditionJson>();

//    public int Priority { get; set; } = 1;

//    public string ExecutionMode { get; set; } = "synchronous";

//    public int Timeout { get; set; } = 5000;

//    public RetryPolicyJson RetryPolicy { get; set; }
//}

//public class ExpressionConditionJson
//{
//    public string When { get; set; }

//    public string Field { get; set; }

//    public string Operator { get; set; } = "equals";

//    public object Value { get; set; }

//    public string Action { get; set; } = "skip"; // "skip", "execute", "fail"
//}

//public class RetryPolicyJson
//{
//    public int MaxAttempts { get; set; } = 3;

//    public int Delay { get; set; } = 1000;

//    public float BackoffMultiplier { get; set; } = 1.5f;

//    public int MaxDelay { get; set; } = 10000;
//}

//public class CompatibilityMatrixJson
//{
//    public string MinRuntimeVersion { get; set; }

//    public List<string> Platforms { get; set; } = new List<string>();

//    public List<DependencyJson> Dependencies { get; set; } = new List<DependencyJson>();

//    public List<string> IncompatibleWith { get; set; } = new List<string>();

//    public Dictionary<string, object> Constraints { get; set; } = new Dictionary<string, object>();
//}

//public class DependencyJson
//{
//    public string Name { get; set; }

//    public string Version { get; set; }

//    public string Type { get; set; } = "gene"; // "gene", "library", "service"
//}

//public class ImmuneResponseJson
//{
//    public string ErrorHandling { get; set; } = "propagate";

//    public int MaxRetries { get; set; } = 3;

//    public int RetryDelay { get; set; } = 1000;

//    public FallbackJson Fallback { get; set; }

//    public CircuitBreakerJson CircuitBreaker { get; set; }

//    public HealthCheckJson HealthCheck { get; set; }
//}

//public class FallbackJson
//{
//    public string Operation { get; set; }

//    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

//    public object DefaultValue { get; set; }
//}

//public class CircuitBreakerJson
//{
//    public int FailureThreshold { get; set; } = 5;

//    public int SuccessThreshold { get; set; } = 2;

//    public int Timeout { get; set; } = 30000;

//    public int HalfOpenMaxCalls { get; set; } = 3;
//}

//public class HealthCheckJson
//{
//    public string Endpoint { get; set; }

//    public int Interval { get; set; } = 30000;

//    public int Timeout { get; set; } = 5000;
//}

//public class ExecutableComponentJson
//{
//    public string Type { get; set; } = "script"; // "assembly", "script", "container", "http", "grpc"

//    public string Language { get; set; } = "javascript"; // "javascript", "python", "csharp", "powershell"

//    public string Source { get; set; }

//    public string EntryPoint { get; set; }

//    public string Assembly { get; set; }

//    public ContainerSpecJson Container { get; set; }

//    public HttpEndpointJson Http { get; set; }

//    public GrpcEndpointJson Grpc { get; set; }

//    public Dictionary<string, object> Config { get; set; } = new Dictionary<string, object>();

//    public Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();
//}

//public class ContainerSpecJson
//{
//    public string Image { get; set; }

//    public List<string> Command { get; set; } = new List<string>();

//    public List<string> Args { get; set; } = new List<string>();

//    public Dictionary<string, string> Env { get; set; } = new Dictionary<string, string>();

//    public ResourceRequirementsJson Resources { get; set; }

//    public List<ContainerPortJson> Ports { get; set; } = new List<ContainerPortJson>();
//}

//public class ResourceRequirementsJson
//{
//    public Dictionary<string, string> Requests { get; set; } = new Dictionary<string, string>();

//    public Dictionary<string, string> Limits { get; set; } = new Dictionary<string, string>();
//}

//public class ContainerPortJson
//{
//    public int ContainerPort { get; set; }

//    public string Protocol { get; set; } = "TCP";
//}

//public class HttpEndpointJson
//{
//    public string Url { get; set; }

//    public string Method { get; set; } = "POST";

//    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

//    public int Timeout { get; set; } = 30000;

//    public bool Retry { get; set; } = true;
//}

//public class GrpcEndpointJson
//{
//    public string Service { get; set; }

//    public string Method { get; set; }

//    public string Address { get; set; }

//    public int Timeout { get; set; } = 30000;
//}

//public class ValidationRuleJson
//{
//    public string Field { get; set; }

//    public string Rule { get; set; } // "required", "regex", "min", "max", "custom"

//    public object Value { get; set; }

//    public string Message { get; set; }
//}

public class ChromosomeJson
{
    public string ApiVersion { get; set; } = "chromosome/v1";

    public string Kind { get; set; } = "Chromosome";

    public ChromosomeMetadata Metadata { get; set; }

    public ChromosomeSpecification Specification { get; set; }
}

public class ChromosomeMetadata
{
    public string Name { get; set; }

    public string Namespace { get; set; } = "default";

    public string Id { get; set; }

    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();

    public DateTimeOffset CreatedAt { get; set; }

    public bool Shared { get; set; } = false;
}

//public class ChromosomeSpec
//{
//    public string Description { get; set; }

//    public List<GeneInstanceJson> Genes { get; set; } = new List<GeneInstanceJson>();

//    public CellularEnvironmentJson Environment { get; set; }

//    public ChromosomeValidationJson Validation { get; set; }

//    public OutputSpecJson Output { get; set; }
//}

//public class GeneInstanceJson
//{
//    public string Id { get; set; }

//    public GeneReferenceJson GeneRef { get; set; }

//    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

//    public GeneConfigJson Config { get; set; }

//    public List<string> Dependencies { get; set; } = new List<string>();

//    public string When { get; set; } // Condition for execution

//    public RetryPolicyJson Retry { get; set; }

//    public int Timeout { get; set; } = 5000;
//}

//public class GeneReferenceJson
//{
//    public string Name { get; set; }

//    public string Version { get; set; } = "latest";

//    public string Namespace { get; set; } = "default";
//}

//public class GeneConfigJson
//{
//    public string Operation { get; set; }

//    public string Mode { get; set; } = "default";

//    public bool Parallel { get; set; } = false;

//    public int Priority { get; set; } = 1;
//}

//public class CellularEnvironmentJson
//{
//    public ImmuneResponseJson ErrorHandling { get; set; }

//    public ResourceConstraintsJson Resources { get; set; }

//    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

//    public Dictionary<string, object> Shared { get; set; } = new Dictionary<string, object>();

//    public SecurityContextJson Security { get; set; }
//}

//public class ResourceConstraintsJson
//{
//    public string Cpu { get; set; } = "100m";

//    public string Memory { get; set; } = "128Mi";

//    public string Storage { get; set; } = "1Gi";

//    public int MaxParallel { get; set; } = 5;
//}

//public class SecurityContextJson
//{
//    public int? RunAsUser { get; set; }

//    public int? RunAsGroup { get; set; }

//    public List<string> Capabilities { get; set; } = new List<string>();

//    public bool ReadOnlyRootFilesystem { get; set; } = false;
//}

//public class ChromosomeValidationJson
//{
//    public string Schema { get; set; }

//    public List<ValidationRuleJson> Rules { get; set; } = new List<ValidationRuleJson>();
//}

//public class OutputSpecJson
//{
//    public string Format { get; set; } = "json";

//    public string Path { get; set; }

//    public List<OutputVariableJson> Variables { get; set; } = new List<OutputVariableJson>();
//}

//public class OutputVariableJson
//{
//    public string Name { get; set; }

//    public string From { get; set; } // geneId.result.field

//    public string Transform { get; set; } // jsonpath, jq, template
//}

public class GenomeJson
{
    public string ApiVersion { get; set; } = "genome/v1";

    public string Kind { get; set; } = "Genome";

    public GenomeMetadata Metadata { get; set; }

    public GenomeSpecification Specification { get; set; }
}

public class GenomeMetadata
{
    public string Name { get; set; }

    public string Namespace { get; set; } = "default";

    public string Id { get; set; }

    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();

    public DateTimeOffset CreatedAt { get; set; }

    public bool Shared { get; set; } = false;

    public string Owner { get; set; }
}

//public class GenomeSpec
//{
//    public string Description { get; set; }

//    public List<ChromosomeReferenceJson> Chromosomes { get; set; } = new List<ChromosomeReferenceJson>();

//    public GenomeEnvironmentJson Environment { get; set; }

//    public GenomeValidationJson Validation { get; set; }

//    public ExecutionPlanJson Execution { get; set; }

//    public GenomeOutputJson Output { get; set; }
//}

//public class ChromosomeReferenceJson
//{
//    public string Ref { get; set; }

//    public string Name { get; set; }

//    public string Namespace { get; set; } = "default";

//    public string When { get; set; }

//    public bool Parallel { get; set; } = false;

//    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
//}

//public class GenomeEnvironmentJson
//{
//    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

//    public List<SecretReferenceJson> Secrets { get; set; } = new List<SecretReferenceJson>();

//    public List<ConfigMapReferenceJson> ConfigMaps { get; set; } = new List<ConfigMapReferenceJson>();

//    public Dictionary<string, object> Shared { get; set; } = new Dictionary<string, object>();
//}

//public class SecretReferenceJson
//{
//    public string Name { get; set; }

//    public string Namespace { get; set; } = "default";

//    public List<string> Keys { get; set; } = new List<string>();
//}

//public class ConfigMapReferenceJson
//{
//    public string Name { get; set; }

//    public string Namespace { get; set; } = "default";

//    public List<string> Keys { get; set; } = new List<string>();
//}

//public class GenomeValidationJson
//{
//    public string Schema { get; set; }

//    public List<ValidationRuleJson> Rules { get; set; } = new List<ValidationRuleJson>();
//}

//public class ExecutionPlanJson
//{
//    public string Strategy { get; set; } = "sequential"; // "sequential", "parallel", "dependency"

//    public int MaxParallel { get; set; } = 3;

//    public int Timeout { get; set; } = 300000; // 5 minutes

//    public RetryPolicyJson Retry { get; set; }
//}

//public class GenomeOutputJson
//{
//    public string Format { get; set; } = "json";

//    public string Path { get; set; }

//    public List<ArtifactSpecJson> Artifacts { get; set; } = new List<ArtifactSpecJson>();
//}

//public class ArtifactSpecJson
//{
//    public string Name { get; set; }

//    public string Type { get; set; } // "file", "data", "report"

//    public string Path { get; set; }

//    public string Content { get; set; }
//}

public class ExecutionRequest
{
    public string ApiVersion { get; set; } = "execution/v1";

    public string Kind { get; set; } = "ExecutionRequest";

    public ExecutionMetadata Metadata { get; set; }

    public ExecutionSpec Spec { get; set; }
}

public class ExecutionMetadata
{
    public string Id { get; set; }

    public string Namespace { get; set; } = "default";

    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

    public DateTimeOffset CreatedAt { get; set; }
}

public class ExecutionSpec
{
    public ExecutionTarget Target { get; set; }

    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

    public Dictionary<string, object> Environment { get; set; } = new Dictionary<string, object>();

    public int Timeout { get; set; } = 300000;

    public bool DryRun { get; set; } = false;

    public bool Validate { get; set; } = true;
}

public class ExecutionTarget
{
    public string Type { get; set; } // "gene", "chromosome", "genome"

    public string Name { get; set; }

    public string Namespace { get; set; } = "default";

    public string Version { get; set; } = "latest";
}

public class ExecutionResponse
{
    public string ApiVersion { get; set; } = "execution/v1";

    public string Kind { get; set; } = "ExecutionResponse";

    public ExecutionResponseMetadata Metadata { get; set; }

    public ExecutionStatus Status { get; set; }

    public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();

    public List<ExecutionError> Errors { get; set; } = new List<ExecutionError>();

    public List<ExecutionLog> Logs { get; set; } = new List<ExecutionLog>();

    public List<ExecutionArtifact> Artifacts { get; set; } = new List<ExecutionArtifact>();
}

public class ExecutionResponseMetadata
{
    public string Id { get; set; }

    public string ExecutionId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public long Duration { get; set; }
}

public class ExecutionStatus
{
    public string Phase { get; set; } // "pending", "running", "succeeded", "failed", "cancelled"

    public string Message { get; set; }

    public string Reason { get; set; }

    public int Progress { get; set; } // 0-100

    public string Health { get; set; } // "healthy", "degraded", "unhealthy"
}

public class ExecutionError
{
    public string Code { get; set; }

    public string Message { get; set; }

    public string Source { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public bool Recoverable { get; set; } = false;
}

public class ExecutionLog
{
    public string Level { get; set; } // "info", "warn", "error", "debug"

    public string Message { get; set; }

    public string Source { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}

public class ExecutionArtifact
{
    public string Name { get; set; }

    public string Type { get; set; }

    public object Content { get; set; }

    public long Size { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class ValidationResponse
{
    public string ApiVersion { get; set; } = "validation/v1";

    public string Kind { get; set; } = "ValidationResponse";

    public ValidationMetadata Metadata { get; set; }

    public ValidationStatus Status { get; set; }

    public List<ValidationError> Errors { get; set; } = new List<ValidationError>();

    public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
}

public class ValidationMetadata
{
    public string Resource { get; set; }

    public string Namespace { get; set; }

    public DateTimeOffset ValidatedAt { get; set; }
}

public class ValidationStatus
{
    public bool Valid { get; set; }

    public int Score { get; set; } // 0-100

    public string Message { get; set; }
}

public class ValidationError
{
    public string Field { get; set; }

    public string Message { get; set; }

    public string Code { get; set; }

    public string Severity { get; set; } // "error", "fatal"
}

public class ValidationWarning
{
    public string Field { get; set; }

    public string Message { get; set; }

    public string Code { get; set; }

    public string Severity { get; set; } = "warning";
}