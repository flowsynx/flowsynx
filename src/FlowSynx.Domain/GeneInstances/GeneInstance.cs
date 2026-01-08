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
    public Dictionary<string, object> NucleotideSequences { get; private set; }
    public ExpressionProfile ExpressionProfile { get; private set; }
    public List<GeneInstanceId> RegulatoryNetwork { get; private set; }
    public Dictionary<string, object> EpigeneticMarks { get; private set; }
    public GeneBlueprint Blueprint { get; set; }
    public Tenant? Tenant { get; set; }

    private GeneInstance() { }

    public GeneInstance(
        TenantId tenantId,
        string userId,
        GeneInstanceId id,
        GeneBlueprintId geneBlueprintId,
        Dictionary<string, object> nucleotideSequences = null,
        ExpressionProfile expressionProfile = null,
        List<GeneInstanceId> regulatoryNetwork = null)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Id = id ?? throw new ArgumentNullException(nameof(id));
        GeneBlueprintId = geneBlueprintId ?? throw new ArgumentNullException(nameof(geneBlueprintId));
        NucleotideSequences = nucleotideSequences ?? new Dictionary<string, object>();
        ExpressionProfile = expressionProfile ?? new ExpressionProfile(null, new Dictionary<string, object>(), new List<RegulatoryCondition>());
        RegulatoryNetwork = regulatoryNetwork ?? new List<GeneInstanceId>();
        EpigeneticMarks = new Dictionary<string, object>();
    }

    public void UpdateNucleotideSequences(Dictionary<string, object> nucleotideSequences)
    {
        NucleotideSequences = nucleotideSequences ?? throw new ArgumentNullException(nameof(nucleotideSequences));
    }

    public void AddRegulatoryNetwork(GeneInstanceId regulatoryId)
    {
        if (regulatoryId == null)
            throw new ArgumentNullException(nameof(regulatoryId));

        if (!RegulatoryNetwork.Contains(regulatoryId))
            RegulatoryNetwork.Add(regulatoryId);
    }

    public void RemoveRegulatoryNetwork(GeneInstanceId regulatoryId)
    {
        RegulatoryNetwork.Remove(regulatoryId);
    }
}