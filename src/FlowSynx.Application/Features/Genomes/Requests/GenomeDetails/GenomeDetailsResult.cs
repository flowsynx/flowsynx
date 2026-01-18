using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genomes;

namespace FlowSynx.Application.Features.Genomes.Requests.GenomeDetails;

public class GenomeDetailsResult
{
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Description { get; set; }
    public GenomeSpecification Specification { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
}