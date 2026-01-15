using FlowSynx.Domain.Genes;

namespace FlowSynx.Application.Features.Genes.Requests.GeneDetails;

public class GeneDetailsResult
{
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public GeneSpecification Specification { get; set; } = new GeneSpecification();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
    public string? Owner { get; set; }
    public string Status { get; set; } = "active";
    public bool IsShared { get; set; }
}