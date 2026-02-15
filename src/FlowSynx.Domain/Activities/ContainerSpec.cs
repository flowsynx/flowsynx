namespace FlowSynx.Domain.Activities;

public class ContainerSpec
{
    public string Image { get; set; } = string.Empty;
    public List<string> Command { get; set; } = new List<string>();
    public List<string> Args { get; set; } = new List<string>();
    public Dictionary<string, string> Env { get; set; } = new Dictionary<string, string>();
    public ResourceRequirements Resources { get; set; } = new ResourceRequirements();
    public List<ContainerPort> Ports { get; set; } = new List<ContainerPort>();
}

public class ContainerPort
{
    public int Port { get; set; }
    public string Protocol { get; set; } = "TCP";
}