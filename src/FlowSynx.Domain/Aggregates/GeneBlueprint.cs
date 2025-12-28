using FlowSynx.Domain.DomainEvents;
using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.Aggregates;

public class GeneBlueprint : AuditableEntity<GeneBlueprintId>, IAggregateRoot
{
    public string Version { get; private set; }
    public string GeneticBlueprint { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public List<ParameterDefinition> GeneticParameters { get; private set; }
    public ExpressionProfile ExpressionProfile { get; private set; }
    public CompatibilityMatrix CompatibilityMatrix { get; private set; }
    public ImmuneResponse ImmuneResponse { get; private set; }
    public ExecutableComponent ExecutableComponent { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    // Domain invariants
    private GeneBlueprint() { } // For EF Core

    public GeneBlueprint(
        GeneBlueprintId id,
        string version,
        string name,
        string description,
        List<ParameterDefinition> geneticParameters,
        ExpressionProfile expressionProfile,
        CompatibilityMatrix compatibilityMatrix,
        ImmuneResponse immuneResponse,
        ExecutableComponent executableComponent,
        string geneticBlueprint = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        GeneticParameters = geneticParameters ?? new List<ParameterDefinition>();
        ExpressionProfile = expressionProfile ?? throw new ArgumentNullException(nameof(expressionProfile));
        CompatibilityMatrix = compatibilityMatrix ?? throw new ArgumentNullException(nameof(compatibilityMatrix));
        ImmuneResponse = immuneResponse;
        ExecutableComponent = executableComponent ?? throw new ArgumentNullException(nameof(executableComponent));
        GeneticBlueprint = geneticBlueprint;
        Metadata = new Dictionary<string, object>();

        ValidateState();
        AddDomainEvent(new GeneBlueprintCreated(this));
    }

    public void Update(
        string? name = null,
        string? description = null,
        ExpressionProfile? expressionProfile = null,
        ImmuneResponse? immuneResponse = null,
        Dictionary<string, object>? metadata = null)
    {
        if (name != null) Name = name;
        if (description != null) Description = description;
        if (expressionProfile != null) ExpressionProfile = expressionProfile;
        if (immuneResponse != null) ImmuneResponse = immuneResponse;
        if (metadata != null) Metadata = metadata;

        ValidateState();
        AddDomainEvent(new GeneBlueprintUpdated(this));
    }

    private void ValidateState()
    {
        if (string.IsNullOrWhiteSpace(Version))
            throw new DomainException("Gene blueprint version cannot be empty");

        if (string.IsNullOrWhiteSpace(Name))
            throw new DomainException("Gene blueprint name cannot be empty");

        if (ExecutableComponent == null)
            throw new DomainException("Executable component is required");
    }

    public bool HasImmuneResponse() => ImmuneResponse != null;

    public string GetGeneticBlueprintId() =>
        string.IsNullOrEmpty(GeneticBlueprint) ? Id.Value : GeneticBlueprint;
}