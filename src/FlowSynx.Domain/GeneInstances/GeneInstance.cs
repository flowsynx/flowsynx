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
    public Chromosome? Chromosome { get; set; }
}

public class GeneConfig
{
    public string Operation { get; set; }

    public string Mode { get; set; } = "default";

    public bool Parallel { get; set; } = false;

    public int Priority { get; set; } = 1;
}