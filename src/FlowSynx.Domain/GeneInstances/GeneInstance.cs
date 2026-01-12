using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.GeneInstances;

public class GeneInstance : AuditableEntity<Guid>, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; }
    public string GeneId { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public GeneConfig Config { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Guid ChromosomeId { get; set; }
    public int Order { get; set; }

    // Navigation property
    public Chromosome Chromosome { get; set; }

    //public TenantId TenantId { get; set; }
    //public string UserId { get; set; }
    //public GeneBlueprintId GeneBlueprintId { get; set; }
    //public Dictionary<string, object> NucleotideSequences { get; set; }
    //public ExpressionProfile ExpressionProfile { get; set; }
    //public List<GeneInstanceId> RegulatoryNetwork { get; set; }
    //public Dictionary<string, object> EpigeneticMarks { get; set; }
    //public GeneBlueprint Blueprint { get; set; }
    //public Tenant? Tenant { get; set; }

    //public GeneInstance() { }

    ////public GeneInstance(
    ////    TenantId tenantId,
    ////    string userId,
    ////    GeneInstanceId id,
    ////    GeneBlueprintId geneBlueprintId,
    ////    Dictionary<string, object> nucleotideSequences = null,
    ////    ExpressionProfile expressionProfile = null,
    ////    List<GeneInstanceId> regulatoryNetwork = null)
    ////{
    ////    TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
    ////    UserId = userId ?? throw new ArgumentNullException(nameof(userId));
    ////    Id = id ?? throw new ArgumentNullException(nameof(id));
    ////    GeneBlueprintId = geneBlueprintId ?? throw new ArgumentNullException(nameof(geneBlueprintId));
    ////    NucleotideSequences = nucleotideSequences ?? new Dictionary<string, object>();
    ////    ExpressionProfile = expressionProfile ?? new ExpressionProfile(null, new Dictionary<string, object>(), new List<RegulatoryCondition>());
    ////    RegulatoryNetwork = regulatoryNetwork ?? new List<GeneInstanceId>();
    ////    EpigeneticMarks = new Dictionary<string, object>();
    ////}

    //public void UpdateNucleotideSequences(Dictionary<string, object> nucleotideSequences)
    //{
    //    NucleotideSequences = nucleotideSequences ?? throw new ArgumentNullException(nameof(nucleotideSequences));
    //}

    //public void AddRegulatoryNetwork(GeneInstanceId regulatoryId)
    //{
    //    if (regulatoryId == null)
    //        throw new ArgumentNullException(nameof(regulatoryId));

    //    if (!RegulatoryNetwork.Contains(regulatoryId))
    //        RegulatoryNetwork.Add(regulatoryId);
    //}

    //public void RemoveRegulatoryNetwork(GeneInstanceId regulatoryId)
    //{
    //    RegulatoryNetwork.Remove(regulatoryId);
    //}
}

public class GeneConfig
{
    public string Operation { get; set; }

    public string Mode { get; set; } = "default";

    public bool Parallel { get; set; } = false;

    public int Priority { get; set; } = 1;
}