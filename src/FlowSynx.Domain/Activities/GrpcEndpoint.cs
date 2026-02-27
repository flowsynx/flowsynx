namespace FlowSynx.Domain.Activities;

public class GrpcEndpoint
{
    public string Service { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30000;
}