using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.GeneInstances;

public class GeneInstance : AuditableEntity<GeneInstanceId>, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; }
    public GeneBlueprintId GeneBlueprintId { get; private set; }
    public Dictionary<string, object> Parameters { get; private set; }
    public ExpressionConfiguration ExpressionConfiguration { get; private set; }
    public List<GeneInstanceId> Dependencies { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }
    public GeneBlueprint Blueprint { get; set; }
    public Tenant? Tenant { get; set; }

    private GeneInstance() { }

    public GeneInstance(
        GeneInstanceId id,
        GeneBlueprintId geneBlueprintId,
        Dictionary<string, object> parameters = null,
        ExpressionConfiguration expressionConfiguration = null,
        List<GeneInstanceId> dependencies = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        GeneBlueprintId = geneBlueprintId ?? throw new ArgumentNullException(nameof(geneBlueprintId));
        Parameters = parameters ?? new Dictionary<string, object>();
        ExpressionConfiguration = expressionConfiguration ?? new ExpressionConfiguration(null, new Dictionary<string, object>(), new List<ExpressionCondition>());
        Dependencies = dependencies ?? new List<GeneInstanceId>();
        Metadata = new Dictionary<string, object>();
    }

    public void UpdateParameters(Dictionary<string, object> parameters)
    {
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    public void AddDependency(GeneInstanceId dependencyId)
    {
        if (dependencyId == null)
            throw new ArgumentNullException(nameof(dependencyId));

        if (!Dependencies.Contains(dependencyId))
            Dependencies.Add(dependencyId);
    }

    public void RemoveDependency(GeneInstanceId dependencyId)
    {
        Dependencies.Remove(dependencyId);
    }
}