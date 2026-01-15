namespace FlowSynx.Application.Features.Chromosomes.Requests.ChromosomesList;

public class ChromosomesListResult
{
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
}