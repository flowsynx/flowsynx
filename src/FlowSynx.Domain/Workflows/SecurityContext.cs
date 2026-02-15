namespace FlowSynx.Domain.Workflows;

public class SecurityContext
{
    public int? RunAsUser { get; set; }
    public int? RunAsGroup { get; set; }
    public List<string> Capabilities { get; set; } = new List<string>();
    public bool ReadOnlyRootFilesystem { get; set; } = false;
}