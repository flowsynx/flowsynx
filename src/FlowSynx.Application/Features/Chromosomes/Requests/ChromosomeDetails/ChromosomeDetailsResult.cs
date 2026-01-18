using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genes;

namespace FlowSynx.Application.Features.Chromosomes.Requests.ChromosomeDetails;

public class ChromosomeDetailsResult
{
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Description { get; set; }
    public ChromosomeSpecification Specification { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
}